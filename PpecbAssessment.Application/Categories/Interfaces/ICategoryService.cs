using PpecbAssessment.Application.Categories.Dtos;
using PpecbAssessment.Application.Common;

namespace PpecbAssessment.Application.Categories.Interfaces
{
    public interface ICategoryService
    {
        Task<Result<Guid>> CreateCategoryAsync(CreateCategoryRequest request, string userId, string auditUser);
        Task<Result> UpdateCategoryAsync(Guid categoryId, UpdateCategoryRequest request, string userId, string auditUser);
        Task<List<CategoryDto>> GetAllAsync(string userId);
        Task<CategoryDto?> GetByIdAsync(Guid categoryId, string userId);
        Task<List<CategoryLookupDto>> GetLookupAsync(string userId);
    }
}
