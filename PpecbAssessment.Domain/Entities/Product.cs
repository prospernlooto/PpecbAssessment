using PpecbAssessment.Domain.Common;

namespace PpecbAssessment.Domain.Entities
{

    public class Product : BaseAuditableEntity
    {
        public Guid ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string? ImagePath { get; set; }

        public Guid CategoryId { get; set; }
        public Category Category { get; set; } = null!;

        public string UserId { get; set; } = string.Empty;

        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}