namespace PpecbAssessment.Application.Products.Dtos
{
    public class ImportProductsResultDto
    {
        public int TotalRows { get; set; }
        public int ImportedCount { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}
