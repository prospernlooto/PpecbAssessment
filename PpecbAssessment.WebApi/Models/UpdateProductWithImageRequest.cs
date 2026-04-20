namespace PpecbAssessment.WebApi.Models
{
    public class UpdateProductWithImageRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Price { get; set; } = string.Empty;
        public Guid CategoryId { get; set; }
        public string RowVersion { get; set; } = string.Empty;
        public IFormFile? Image { get; set; }
    }
}
