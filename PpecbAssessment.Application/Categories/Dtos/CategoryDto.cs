namespace PpecbAssessment.Application.Categories.Dtos
{
    public class CategoryDto
    {
        public Guid CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string CategoryCode { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string RowVersion { get; set; } = string.Empty;
    }
}
