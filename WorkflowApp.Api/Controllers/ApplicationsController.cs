using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkflowApp.Api.DTOs.Applications;
using WorkflowApp.Api.Infrastructure.Data;
using WorkflowApp.Api.Services.Interfaces;

namespace WorkflowApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ApplicationsController : ControllerBase
    {
        private IApplicationService _service;
        private readonly AppDbContext _dbContext;


        public ApplicationsController(IApplicationService service, AppDbContext dbContext)
        {
            this._service = service;
            this._dbContext = dbContext;
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
        [HttpGet]
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
        /// <param name="none">キャンセルトークン</param>
        /// <returns>更新結果</returns>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, UpdateApplicationRequest request, CancellationToken none)
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

            var application = _dbContext.Applications.FirstOrDefault(x => x.Id == id && x.ApplicantUserId.ToString() == userIdClaim);
            if (application == null)
            {
                return NotFound();
            }

            application.Title = request.Title;
            application.Content = request.Content;
            application.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(none);

            return NoContent();
        }

        /// <summary>
        /// 申請を削除します。認証されたユーザーの申請のみが削除されます。
        /// </summary>
        /// <param name="id">削除する申請のID</param>
        /// <returns>削除結果</returns>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim))
            {
                return Unauthorized();
            }

            var application = await _dbContext.Applications
                .FirstOrDefaultAsync(x => x.Id == id && x.ApplicantUserId.ToString() == userIdClaim);

            if (application is null)
            {
                return NotFound();
            }

            _dbContext.Applications.Remove(application);
            await _dbContext.SaveChangesAsync();

            return NoContent();
        }
    }
}