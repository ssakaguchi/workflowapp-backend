using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkflowApp.Api.Domain.Enums;
using WorkflowApp.Api.DTOs.Applications;
using WorkflowApp.Api.Services.Interfaces;

namespace WorkflowApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ApplicationsController : ControllerBase
    {
        private IApplicationService _service;


        public ApplicationsController(IApplicationService service)
        {
            this._service = service;
        }

        /// <summary>
        /// 新しい申請を作成します。
        /// </summary>
        /// <param name="request">作成する申請の情報</param>
        /// <param name="none">キャンセルトークン</param>
        /// <returns>作成された申請のID</returns>
        [HttpPost]

        public async Task<IActionResult> Create([FromBody] CreateApplicationRequest request,
                                                        CancellationToken none)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var result = await _service.CreateAsync(request,
                                                           userId,
                                                           none);


            return CreatedAtAction(nameof(Create),
                                   new { id = result.Id },
                                   result);
        }

        /// <summary>
        /// 申請の一覧を取得します。認証されたユーザーの申請のみが返されます。
        /// </summary>
        /// <param name="none">キャンセルトークン</param>
        /// <returns>申請の一覧</returns>
        [HttpGet("list")]
        public async Task<IActionResult> GetList(CancellationToken none)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var result = await _service.GetListAsync(userId, none);
            return Ok(result);
        }

        /// <summary>
        /// 申請の詳細を取得します。認証されたユーザーの申請のみが返されます。
        /// </summary>
        /// <param name="id">取得する申請のID</param>
        /// <param name="none">キャンセルトークン</param>
        /// <returns>申請の詳細</returns>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetDetail(int id, CancellationToken none)
        {
            // 認証されたユーザーのIDを取得
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var result = await _service.GetDetailAsync(id, userId, none);
            if (result == null)
            {
                return NotFound();
            }

            return Ok(result);
        }

        /// <summary>
        /// 申請を更新します。認証されたユーザーの申請のみが更新されます。
        /// </summary>
        /// <param name="id">更新する申請のID</param>
        /// <param name="request">更新する申請の情報</param>
        /// <param name="cancellationToken">キャンセルトークン</param>
        /// <returns>更新結果</returns>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, UpdateApplicationRequest request, CancellationToken cancellationToken)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim))
            {
                return Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return BadRequest("タイトルは必須です。");
            }

            if (string.IsNullOrWhiteSpace(request.Content))
            {
                return BadRequest("内容は必須です。");
            }

            var isUpdated = await _service.UpdateAsync(id, request, userIdClaim, cancellationToken);
            if (!isUpdated)
            {
                return NotFound();
            }

            return NoContent();
        }

        /// <summary>
        /// 申請を削除します。認証されたユーザーの申請のみが削除されます。
        /// </summary>
        /// <param name="id">削除する申請のID</param>
        /// <returns>削除結果</returns>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim))
            {
                return Unauthorized();
            }

            var isDeleted = await _service.DeleteAsync(id, userIdClaim, cancellationToken);

            if (!isDeleted)
            {
                return NotFound();
            }

            return NoContent();
        }

        /// <summary>
        /// 申請のステータスを更新します。認証されたユーザーの申請のみが更新されます。
        /// </summary>
        /// <param name="id">更新する申請のID</param>
        /// <param name="request">更新する申請の情報</param>
        /// <param name="cancellationToken">キャンセルトークン</param>
        /// <returns>更新結果</returns>
        [HttpPatch("{id:int}/status")]
        public async Task<IActionResult> UpdateWorkflowStatus(int id, UpdateWorkflowStatusRequest request, CancellationToken cancellationToken)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            // ステータスの検証とパース
            if (string.IsNullOrWhiteSpace(request.Status) ||
                !Enum.TryParse<WorkflowStatus>(request.Status, ignoreCase: false, out var status))
            {
                return BadRequest("無効なステータスです。");
            }

            var isUpdated = await _service.UpdateWorkflowStatusAsync(id, status, userId, cancellationToken);
            if (!isUpdated)
            {
                return NotFound();
            }

            return NoContent();
        }

        /// <summary>
        /// 申請の一覧をページネーション付きで取得します。
        /// </summary>
        /// <param name="page">取得するページ番号</param>
        /// <param name="pageSize">1ページあたりの件数</param>
        /// <param name="status">フィルタリングするステータス（省略可能）</param>
        /// <param name="cancellationToken">キャンセルトークン</param>
        /// <returns>ページネーションされた申請の一覧</returns>
        [HttpGet]
        public async Task<ActionResult<PagedResponse<ApplicationListItemResponse>>> GetApplications(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? status = null,
            CancellationToken cancellationToken = default)
        {
            if (page < 1) { page = 1; }

            if (pageSize < 1) { pageSize = 10; }

            if (pageSize > 100) { pageSize = 100; }

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var result = await _service.GetApplicationsAsync(page, pageSize, status?.Trim(), userId, cancellationToken);

            return Ok(result);
        }
    }
}