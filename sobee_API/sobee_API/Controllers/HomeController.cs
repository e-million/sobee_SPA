using Microsoft.AspNetCore.Mvc;
using Sobee.Domain.Data; 

namespace sobee_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HomeController : ControllerBase
    {

        private readonly SobeecoredbContext _db;

        public HomeController(SobeecoredbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Health check for API and database connectivity.
        /// </summary>
        [HttpGet("ping")]
        public IActionResult Ping() {
            var canConnect = _db.Database.CanConnect();
            return Ok(new { status = "ok", db = canConnect });
        
        }






    }
}
