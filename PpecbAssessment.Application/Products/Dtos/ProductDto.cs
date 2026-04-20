namespace PpecbAssessment.Application.Products.Dtos
{
   public class ProductDto
    {
        public Guid ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string? ImagePath { get; set; }
        public string RowVersion { get; set; } = string.Empty;
    }
}
