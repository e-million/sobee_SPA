using Microsoft.AspNetCore.Mvc;
using sobee_API.Domain;
using sobee_API.Services.Interfaces;

namespace sobee_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HomeController : ApiControllerBase
    {
        private readonly IHomeService _homeService;

        public HomeController(IHomeService homeService)
        {
            _homeService = homeService;
        }

        /// <summary>
        /// Health check for API and database connectivity.
        /// </summary>
        [HttpGet("ping")]
        public async Task<IActionResult> Ping()
        {
            var result = await _homeService.GetPingAsync();
            return FromServiceResult(result);
        }

    }
}
