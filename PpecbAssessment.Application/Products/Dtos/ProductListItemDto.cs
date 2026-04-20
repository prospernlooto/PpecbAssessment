namespace PpecbAssessment.Application.Products.Dtos
{
    public class ProductListItemDto
    {
        public Guid ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string? ImagePath { get; set; }
    }
}
