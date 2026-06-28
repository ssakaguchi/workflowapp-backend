using System.Linq.Expressions;
using WorkflowApp.Api.Domain.Entities;

namespace WorkflowApp.Api.DTOs.Applications
{
    public class ApplicationListItemResponse
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int ApplicantUserId { get; set; }
        public string ApplicantDisplayName { get; init; } = string.Empty;

        /// <summary>
        /// ApplicationエンティティからApplicationListItemResponseへの変換式を定義します。
        /// </summary>
        public static readonly Expression<Func<Application, ApplicationListItemResponse>> Projection =
            application => new ApplicationListItemResponse
            {
                Id = application.Id,
                Title = application.Title,
                Status = application.Status.ToString(),
                CreatedAt = application.CreatedAt,
                ApplicantUserId = application.ApplicantUserId,
                ApplicantDisplayName = application.ApplicantUser.DisplayName
            };
    }
}
