namespace PpecbAssessment.Domain.Entities
{
    public class AppUser
    {
        public Guid AppUserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
    }
}
