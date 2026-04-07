using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    }
}