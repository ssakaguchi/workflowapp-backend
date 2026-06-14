using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkflowApp.Api.CustomException;
using WorkflowApp.Api.Services.Interfaces;

namespace WorkflowApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {

        private readonly IUserService _service;

        public UsersController(IUserService service)
        {
            _service = service;
        }

        /// <summary>
        /// 承認者のリストを取得するエンドポイント
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("approvers")]
        public async Task<IActionResult> GetApprovers(CancellationToken cancellationToken)
        {
            try
            {
                var result = await _service.GetApproversAsync(cancellationToken);
                if (result == null || result.Count == 0)
                {
                    return NotFound();
                }

                return Ok(result);
            }
            catch (ApproverNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }
    }
}
