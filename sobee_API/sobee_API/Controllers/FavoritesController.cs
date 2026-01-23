using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sobee_API.Domain;
using sobee_API.Services;
using sobee_API.Services.Interfaces;

namespace sobee_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FavoritesController : ApiControllerBase
    {
        private readonly IFavoriteService _favoriteService;
        private readonly RequestIdentityResolver _identity;

        public FavoritesController(IFavoriteService favoriteService, RequestIdentityResolver identity)
        {
            _favoriteService = favoriteService;
            _identity = identity;
        }

        /// <summary>
        /// List favorites for the authenticated user.
        /// </summary>
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetMyFavorites(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            // Auth-only endpoint: do not create guest sessions.
            var (identity, errorResult) = await ResolveIdentityAsync();
            if (errorResult != null)
            {
                return errorResult;
            }

            var result = await _favoriteService.GetFavoritesAsync(identity!.UserId, page, pageSize);
            if (!result.Success)
            {
                return FromServiceResult(result);
            }

            Response.Headers["X-Total-Count"] = result.Value!.TotalCount.ToString();
            Response.Headers["X-Page"] = page.ToString();
            Response.Headers["X-Page-Size"] = pageSize.ToString();

            return Ok(result.Value);
        }

        /// <summary>
        /// Add a product to favorites for the authenticated user.
        /// </summary>
        [HttpPost("{productId:int}")]
        [Authorize]
        public async Task<IActionResult> AddFavorite(int productId)
        {
            var (identity, errorResult) = await ResolveIdentityAsync();
            if (errorResult != null)
            {
                return errorResult;
            }

            var result = await _favoriteService.AddFavoriteAsync(identity!.UserId, productId);
            return FromServiceResult(result);
        }

        /// <summary>
        /// Remove a product from favorites for the authenticated user.
        /// </summary>
        [HttpDelete("{productId:int}")]
        [Authorize]
        public async Task<IActionResult> RemoveFavorite(int productId)
        {
            var (identity, errorResult) = await ResolveIdentityAsync();
            if (errorResult != null)
            {
                return errorResult;
            }

            var result = await _favoriteService.RemoveFavoriteAsync(identity!.UserId, productId);
            return FromServiceResult(result);
        }

        private async Task<(RequestIdentity? identity, IActionResult? errorResult)> ResolveIdentityAsync()
        {
            var identity = await _identity.ResolveAsync(
                User,
                Request,
                Response,
                allowCreateGuestSession: false,
                allowAuthenticatedGuestSession: false);

            if (identity.HasError)
            {
                if (identity.ErrorCode == "MissingNameIdentifier")
                {
                    return (null, UnauthorizedError(identity.ErrorMessage ?? "Unauthorized", "Unauthorized"));
                }

                return (null, BadRequestError(identity.ErrorMessage ?? "Invalid request", "ValidationError"));
            }

            return (identity, null);
        }

    }
}
