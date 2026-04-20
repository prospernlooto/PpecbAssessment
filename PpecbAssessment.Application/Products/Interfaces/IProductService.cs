using Microsoft.AspNetCore.Http;
using PpecbAssessment.Application.Common;
using PpecbAssessment.Application.Products.Dtos;

namespace PpecbAssessment.Application.Products.Interfaces
{
    public interface IProductService
    {
        Task<Result<Guid>> CreateProductAsync(
            CreateProductRequest request,
            string userId,
            string auditUser,
            string uploadsRootPath,
            IFormFile? image = null);

        Task<Result> UpdateProductAsync(
     Guid productId,
     UpdateProductRequest request,
     string userId,
     string auditUser,
     string uploadsRootPath,
     IFormFile? image = null);

        Task<ProductDto?> GetByIdAsync(Guid productId, string userId);

        Task<PagedResult<ProductListItemDto>> GetPagedAsync(string userId, int pageNumber);

        Task<Result> DeleteProductAsync(Guid productId, string userId);

        Task<Result<ImportProductsResultDto>> ImportProductsAsync(IFormFile file, string userId, string auditUser);

        Task<byte[]> ExportProductsAsync(string userId);
    }
}
