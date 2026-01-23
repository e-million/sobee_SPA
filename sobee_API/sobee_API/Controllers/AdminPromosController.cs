using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sobee_API.Domain;
using sobee_API.DTOs.Admin;
using sobee_API.Services.Interfaces;

namespace sobee_API.Controllers
{
    [ApiController]
    [Route("api/admin/promos")]
    [Authorize(Roles = "Admin")]
    public class AdminPromosController : ApiControllerBase
    {
        private readonly IAdminPromoService _promoService;

        public AdminPromosController(IAdminPromoService promoService)
        {
            _promoService = promoService;
        }

        [HttpGet]
        public async Task<IActionResult> GetPromos(
            [FromQuery] string? search,
            [FromQuery] bool includeExpired = false,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var result = await _promoService.GetPromosAsync(search, includeExpired, page, pageSize);
            return FromServiceResult(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreatePromo([FromBody] CreatePromoRequest request)
        {
            var result = await _promoService.CreatePromoAsync(request);
            return FromServiceResult(result);
        }

        [HttpPut("{promoId:int}")]
        public async Task<IActionResult> UpdatePromo(int promoId, [FromBody] UpdatePromoRequest request)
        {
            var result = await _promoService.UpdatePromoAsync(promoId, request);
            return FromServiceResult(result);
        }

        [HttpDelete("{promoId:int}")]
        public async Task<IActionResult> DeletePromo(int promoId)
        {
            var result = await _promoService.DeletePromoAsync(promoId);
            return FromServiceResult(result);
        }

        private IActionResult FromServiceResult<T>(ServiceResult<T> result)
        {
            if (result.Success)
            {
                return Ok(result.Value);
            }

            var code = result.ErrorCode ?? "ServerError";
            var message = result.ErrorMessage ?? "An unexpected error occurred.";

            return code switch
            {
                "NotFound" => NotFoundError(message, code, result.ErrorData),
                "ValidationError" => BadRequestError(message, code, result.ErrorData),
                "Unauthorized" => UnauthorizedError(message, code, result.ErrorData),
                "Forbidden" => ForbiddenError(message, code, result.ErrorData),
                "Conflict" => ConflictError(message, code, result.ErrorData),
                _ => ServerError(message, code, result.ErrorData)
            };
        }
    }
}
