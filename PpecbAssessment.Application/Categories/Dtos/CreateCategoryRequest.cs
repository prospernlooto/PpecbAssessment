namespace PpecbAssessment.Application.Categories.Dtos
{
    public class CreateCategoryRequest
    {
        public string Name { get; set; } = string.Empty;
        public string CategoryCode { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
