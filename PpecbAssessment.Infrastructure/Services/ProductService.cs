using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PpecbAssessment.Application.Common;
using PpecbAssessment.Application.Products.Dtos;
using PpecbAssessment.Application.Products.Interfaces;
using PpecbAssessment.Domain.Entities;
using PpecbAssessment.Infrastructure.Persistence;

namespace PpecbAssessment.Infrastructure.Services
{
    public class ProductService : IProductService
    {
        private readonly AppDbContext _context;

        public ProductService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Result<Guid>> CreateProductAsync(
            CreateProductRequest request,
            string userId,
            string auditUser,
            string uploadsRootPath,
            IFormFile? image = null)
        {
            var category = await _context.Categories
                .FirstOrDefaultAsync(x => x.CategoryId == request.CategoryId
                                       && x.UserId == userId
                                       && x.IsActive);

            if (category == null)
                return Result<Guid>.Failure("Selected category was not found.");

            const int maxRetries = 3;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                var productCode = await GenerateNextProductCodeAsync(userId);

                string? imagePath = null;
                if (image != null && image.Length > 0)
                {
                    imagePath = await SaveImageAsync(image, uploadsRootPath);
                }

                var product = new Product
                {
                    ProductId = Guid.NewGuid(),
                    ProductCode = productCode,
                    Name = request.Name.Trim(),
                    Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                    Price = request.Price,
                    CategoryId = request.CategoryId,
                    UserId = userId,
                    ImagePath = imagePath,
                    CreatedBy = auditUser,
                    CreatedDate = DateTime.UtcNow
                };

                _context.Products.Add(product);

                try
                {
                    await _context.SaveChangesAsync();
                    return Result<Guid>.Success(product.ProductId, "Product created successfully.");
                }
                catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
                {
                    _context.Entry(product).State = EntityState.Detached;

                    if (attempt == maxRetries)
                        return Result<Guid>.Failure("Could not generate a unique product code. Please try again.");
                }
            }

            return Result<Guid>.Failure("Could not create product.");
        }

        public async Task<Result> UpdateProductAsync(
            Guid productId,
            UpdateProductRequest request,
            string userId,
            string auditUser,
            string uploadsRootPath,
            IFormFile? image = null)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(x => x.ProductId == productId && x.UserId == userId);

            if (product == null)
                return Result.Failure("Product not found.");

            var category = await _context.Categories
                .FirstOrDefaultAsync(x => x.CategoryId == request.CategoryId
                                       && x.UserId == userId
                                       && x.IsActive);

            if (category == null)
                return Result.Failure("Selected category was not found.");

            product.Name = request.Name.Trim();
            product.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
            product.Price = request.Price;
            product.CategoryId = request.CategoryId;
            product.UpdatedBy = auditUser;
            product.UpdatedDate = DateTime.UtcNow;

            if (image != null && image.Length > 0)
            {
                var newImagePath = await SaveImageAsync(image, uploadsRootPath);
                product.ImagePath = newImagePath;
            }

            var originalRowVersion = Convert.FromBase64String(request.RowVersion);
            _context.Entry(product).Property(x => x.RowVersion).OriginalValue = originalRowVersion;

            try
            {
                await _context.SaveChangesAsync();
                return Result.Success("Product updated successfully.");
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Conflict("This product was updated by another user. Please reload and try again.");
            }
        }

        public async Task<ProductDto?> GetByIdAsync(Guid productId, string userId)
        {
            return await _context.Products
                .AsNoTracking()
                .Where(x => x.ProductId == productId && x.UserId == userId)
                .Select(x => new ProductDto
                {
                    ProductId = x.ProductId,
                    ProductCode = x.ProductCode,
                    Name = x.Name,
                    Description = x.Description,
                    Price = x.Price,
                    CategoryId = x.CategoryId,
                    CategoryName = x.Category.Name,
                    ImagePath = x.ImagePath,
                    RowVersion = Convert.ToBase64String(x.RowVersion)
                })
                .FirstOrDefaultAsync();
        }

        public async Task<PagedResult<ProductListItemDto>> GetPagedAsync(string userId, int pageNumber)
        {
            const int pageSize = 10;

            if (pageNumber < 1)
                pageNumber = 1;

            var query = _context.Products
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CreatedDate);

            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new ProductListItemDto
                {
                    ProductId = x.ProductId,
                    ProductCode = x.ProductCode,
                    Name = x.Name,
                    Price = x.Price,
                    CategoryName = x.Category.Name,
                    ImagePath = x.ImagePath
                })
                .ToListAsync();

            return new PagedResult<ProductListItemDto>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
        }

        public async Task<Result> DeleteProductAsync(Guid productId, string userId)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(x => x.ProductId == productId && x.UserId == userId);

            if (product == null)
                return Result.Failure("Product not found.");

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return Result.Success("Product deleted successfully.");
        }

        public async Task<Result<ImportProductsResultDto>> ImportProductsAsync(IFormFile file, string userId, string auditUser)
        {
            if (file == null || file.Length == 0)
                return Result<ImportProductsResultDto>.Failure("Please upload a valid Excel file.");

            var result = new ImportProductsResultDto();

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0;

            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheets.First();

            var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
            if (lastRow < 2)
                return Result<ImportProductsResultDto>.Failure("The Excel file does not contain any data rows.");

            for (int row = 2; row <= lastRow; row++)
            {
                result.TotalRows++;

                try
                {
                    var name = worksheet.Cell(row, 1).GetString().Trim();
                    var description = worksheet.Cell(row, 2).GetString().Trim();
                    var priceText = worksheet.Cell(row, 3).GetString().Trim();
                    var categoryCode = worksheet.Cell(row, 4).GetString().Trim().ToUpperInvariant();

                    if (string.IsNullOrWhiteSpace(name))
                    {
                        result.Errors.Add($"Row {row}: Name is required.");
                        continue;
                    }

                    if (!decimal.TryParse(priceText, out var price) || price < 0)
                    {
                        result.Errors.Add($"Row {row}: Price must be a valid non-negative number.");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(categoryCode))
                    {
                        result.Errors.Add($"Row {row}: CategoryCode is required.");
                        continue;
                    }

                    var category = await _context.Categories
                        .FirstOrDefaultAsync(x => x.UserId == userId
                                               && x.CategoryCode == categoryCode
                                               && x.IsActive);

                    if (category == null)
                    {
                        result.Errors.Add($"Row {row}: Category '{categoryCode}' was not found.");
                        continue;
                    }

                    const int maxRetries = 3;
                    var created = false;

                    for (int attempt = 1; attempt <= maxRetries; attempt++)
                    {
                        var productCode = await GenerateNextProductCodeAsync(userId);

                        var product = new Product
                        {
                            ProductId = Guid.NewGuid(),
                            ProductCode = productCode,
                            Name = name,
                            Description = string.IsNullOrWhiteSpace(description) ? null : description,
                            Price = price,
                            CategoryId = category.CategoryId,
                            UserId = userId,
                            CreatedBy = auditUser,
                            CreatedDate = DateTime.UtcNow
                        };

                        _context.Products.Add(product);

                        try
                        {
                            await _context.SaveChangesAsync();
                            result.ImportedCount++;
                            created = true;
                            break;
                        }
                        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
                        {
                            _context.Entry(product).State = EntityState.Detached;

                            if (attempt == maxRetries)
                            {
                                result.Errors.Add($"Row {row}: Could not generate a unique product code.");
                            }
                        }
                    }

                    if (!created)
                        continue;
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Row {row}: {ex.Message}");
                }
            }

            return Result<ImportProductsResultDto>.Success(result, "Import completed.");
        }

        public async Task<byte[]> ExportProductsAsync(string userId)
        {
            var products = await _context.Products
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CreatedDate)
                .Select(x => new
                {
                    x.ProductCode,
                    x.Name,
                    x.Description,
                    x.Price,
                    CategoryName = x.Category.Name,
                    CategoryCode = x.Category.CategoryCode,
                    x.CreatedDate
                })
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Products");

            worksheet.Cell(1, 1).Value = "ProductCode";
            worksheet.Cell(1, 2).Value = "Name";
            worksheet.Cell(1, 3).Value = "Description";
            worksheet.Cell(1, 4).Value = "Price";
            worksheet.Cell(1, 5).Value = "CategoryName";
            worksheet.Cell(1, 6).Value = "CategoryCode";
            worksheet.Cell(1, 7).Value = "CreatedDate";

            for (int i = 0; i < products.Count; i++)
            {
                var row = i + 2;
                var item = products[i];

                worksheet.Cell(row, 1).Value = item.ProductCode;
                worksheet.Cell(row, 2).Value = item.Name;
                worksheet.Cell(row, 3).Value = item.Description;
                worksheet.Cell(row, 4).Value = item.Price;
                worksheet.Cell(row, 5).Value = item.CategoryName;
                worksheet.Cell(row, 6).Value = item.CategoryCode;
                worksheet.Cell(row, 7).Value = item.CreatedDate;
            }

            var headerRange = worksheet.Range(1, 1, 1, 7);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            worksheet.Column(4).Style.NumberFormat.Format = "#,##0.00";
            worksheet.Column(7).Style.DateFormat.Format = "yyyy-mm-dd HH:mm:ss";

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        private async Task<string> GenerateNextProductCodeAsync(string userId)
        {
            var prefix = DateTime.UtcNow.ToString("yyyyMM");

            var lastCode = await _context.Products
                .Where(x => x.UserId == userId && x.ProductCode.StartsWith(prefix + "-"))
                .OrderByDescending(x => x.ProductCode)
                .Select(x => x.ProductCode)
                .FirstOrDefaultAsync();

            var nextNumber = 1;

            if (!string.IsNullOrWhiteSpace(lastCode))
            {
                var parts = lastCode.Split('-');
                if (parts.Length == 2 && int.TryParse(parts[1], out var currentNumber))
                {
                    nextNumber = currentNumber + 1;
                }
            }

            return $"{prefix}-{nextNumber:D3}";
        }

        private static bool IsUniqueConstraintViolation(DbUpdateException ex)
        {
            if (ex.InnerException is SqlException sqlEx)
            {
                return sqlEx.Number == 2601 || sqlEx.Number == 2627;
            }

            return false;
        }

        private async Task<string> SaveImageAsync(IFormFile file, string uploadsRootPath)
        {
            var uploadsFolder = Path.Combine(uploadsRootPath, "products");

            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var extension = Path.GetExtension(file.FileName);
            var fileName = $"{Guid.NewGuid()}{extension}";
            var fullPath = Path.Combine(uploadsFolder, fileName);

            await using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream);

            return $"/uploads/products/{fileName}";
        }
    }
}