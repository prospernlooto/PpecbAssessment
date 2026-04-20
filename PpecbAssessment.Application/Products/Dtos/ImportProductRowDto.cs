namespace PpecbAssessment.Application.Products.Dtos
{
    public class ImportProductRowDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string CategoryCode { get; set; } = string.Empty;
    }
}
