namespace PpecbAssessment.Application.Products.Dtos
{
    public class UpdateProductRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public Guid CategoryId { get; set; }
        public string RowVersion { get; set; } = string.Empty;
    }
}
