using System.ComponentModel.DataAnnotations;

namespace WorkflowApp.Api.DTOs.Applications
{
    /// <summary>
    /// ワークフロー申請の新規作成リクエスト
    /// </summary>
    public class CreateApplicationRequest
    {
        [Required(ErrorMessage = "件名は必須です。")]
        [MinLength(1, ErrorMessage = "タイトルは必須です。")]
        [MaxLength(100, ErrorMessage = "件名は100文字以内で入力してください。")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "申請内容は必須です。")]
        [MinLength(1, ErrorMessage = "内容は必須です。")]
        [MaxLength(2000, ErrorMessage = "申請内容は2000文字以内で入力してください。")]
        public string Content { get; set; } = string.Empty;
    }
}
