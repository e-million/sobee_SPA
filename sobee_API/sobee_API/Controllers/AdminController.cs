using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace sobee_API.Controllers
{
    [ApiController]
    [Route("api/admin")]
    public class AdminController : ControllerBase
    {
        [HttpGet("ping")]
        [Authorize(Roles = "Admin")]
        public IActionResult Ping()
        {
            return Ok(new { message = "Admin ping ok." });
        }
    }
}
