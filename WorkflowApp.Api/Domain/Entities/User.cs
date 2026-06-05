using WorkflowApp.Api.Domain.Enums;

namespace WorkflowApp.Api.Domain.Entities
{
    public class User
    {
        public int Id { get; set; }
        
        public string LoginId { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public UserRole Role { get; set; } = UserRole.Applicant;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
