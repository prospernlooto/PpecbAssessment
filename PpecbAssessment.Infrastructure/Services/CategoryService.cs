using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PpecbAssessment.Application.Categories.Dtos;
using PpecbAssessment.Application.Categories.Interfaces;
using PpecbAssessment.Application.Common;
using PpecbAssessment.Domain.Entities;
using PpecbAssessment.Infrastructure.Persistence;
using System.Text.RegularExpressions;

namespace PpecbAssessment.Infrastructure.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly AppDbContext _context;

        public CategoryService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Result<Guid>> CreateCategoryAsync(CreateCategoryRequest request, string userId, string auditUser)
        {
            var code = request.CategoryCode.Trim().ToUpperInvariant();

            if (!Regex.IsMatch(code, "^[A-Z]{3}[0-9]{3}$"))
                return Result<Guid>.Failure("Category code must be in the format ABC123.");

            var exists = await _context.Categories
                .AnyAsync(x => x.UserId == userId && x.CategoryCode == code);

            if (exists)
                return Result<Guid>.Failure("Category code already exists.");

            var category = new Category
            {
                CategoryId = Guid.NewGuid(),
                Name = request.Name.Trim(),
                CategoryCode = code,
                IsActive = request.IsActive,
                UserId = userId,
                CreatedBy = auditUser,
                CreatedDate = DateTime.UtcNow
            };

            _context.Categories.Add(category);

            try
            {
                await _context.SaveChangesAsync();
                return Result<Guid>.Success(category.CategoryId, "Category created successfully.");
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                return Result<Guid>.Failure("Category code already exists.");
            }
        }

        public async Task<Result> UpdateCategoryAsync(Guid categoryId, UpdateCategoryRequest request, string userId, string auditUser)
        {
            var category = await _context.Categories
                .FirstOrDefaultAsync(x => x.CategoryId == categoryId && x.UserId == userId);

            if (category == null)
                return Result.Failure("Category not found.");

            var code = request.CategoryCode.Trim().ToUpperInvariant();

            if (!Regex.IsMatch(code, "^[A-Z]{3}[0-9]{3}$"))
                return Result.Failure("Category code must be in the format ABC123.");

            var duplicate = await _context.Categories
                .AnyAsync(x => x.UserId == userId
                            && x.CategoryId != categoryId
                            && x.CategoryCode == code);

            if (duplicate)
                return Result.Failure("Category code already exists.");

            category.Name = request.Name.Trim();
            category.CategoryCode = code;
            category.IsActive = request.IsActive;
            category.UpdatedBy = auditUser;
            category.UpdatedDate = DateTime.UtcNow;

            var originalRowVersion = Convert.FromBase64String(request.RowVersion);
            _context.Entry(category).Property(x => x.RowVersion).OriginalValue = originalRowVersion;

            try
            {
                await _context.SaveChangesAsync();
                return Result.Success("Category updated successfully.");
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Conflict("This category was updated by another user. Please reload and try again.");
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                return Result.Failure("Category code already exists.");
            }
        }

        public async Task<List<CategoryDto>> GetAllAsync(string userId)
        {
            return await _context.Categories
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .OrderBy(x => x.Name)
                .Select(x => new CategoryDto
                {
                    CategoryId = x.CategoryId,
                    Name = x.Name,
                    CategoryCode = x.CategoryCode,
                    IsActive = x.IsActive,
                    RowVersion = Convert.ToBase64String(x.RowVersion)
                })
                .ToListAsync();
        }

        public async Task<CategoryDto?> GetByIdAsync(Guid categoryId, string userId)
        {
            return await _context.Categories
                .AsNoTracking()
                .Where(x => x.CategoryId == categoryId && x.UserId == userId)
                .Select(x => new CategoryDto
                {
                    CategoryId = x.CategoryId,
                    Name = x.Name,
                    CategoryCode = x.CategoryCode,
                    IsActive = x.IsActive,
                    RowVersion = Convert.ToBase64String(x.RowVersion)
                })
                .FirstOrDefaultAsync();
        }

        public async Task<List<CategoryLookupDto>> GetLookupAsync(string userId)
        {
            return await _context.Categories
                .AsNoTracking()
                .Where(x => x.UserId == userId && x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new CategoryLookupDto
                {
                    CategoryId = x.CategoryId,
                    Name = x.Name
                })
                .ToListAsync();
        }

        private static bool IsUniqueConstraintViolation(DbUpdateException ex)
        {
            if (ex.InnerException is SqlException sqlEx)
            {
                return sqlEx.Number == 2601 || sqlEx.Number == 2627;
            }

            return false;
        }
    }
}
