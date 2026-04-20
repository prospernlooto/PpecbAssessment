using PpecbAssessment.Domain.Common;

namespace PpecbAssessment.Domain.Entities
{

    public class Category : BaseAuditableEntity
    {
        public Guid CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string CategoryCode { get; set; } = string.Empty;
        public bool IsActive { get; set; }

        public string UserId { get; set; } = string.Empty;

        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}