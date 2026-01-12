using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sobee.Domain.Data;
using Sobee.Domain.Entities.Cart; // if needed by your project; safe to remove if unused
using Sobee.Domain.Entities.Products;
using sobee_API.DTOs.Favorites;
using sobee_API.Services;
using System.Security.Claims;

namespace sobee_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FavoritesController : ControllerBase
    {
        private readonly SobeecoredbContext _db;
        private readonly RequestIdentityResolver _identity;

        public FavoritesController(SobeecoredbContext db, RequestIdentityResolver identity)
        {
            _db = db;
            _identity = identity;
        }

        /// <summary>
        /// List favorites for the authenticated user.
        /// </summary>
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetMyFavorites()
        {
            // Auth-only endpoint: do not create guest sessions.
            var owner = await _identity.ResolveAsync(
                User,
                Request,
                Response,
                allowCreateGuestSession: false,
                allowAuthenticatedGuestSession: false
            );

            if (string.IsNullOrWhiteSpace(owner.UserId))
                return Unauthorized(new { error = "Missing NameIdentifier claim." });

            var favorites = await _db.Tfavorites
                .Where(f => f.UserId == owner.UserId)
                .OrderByDescending(f => f.DtmDateAdded)
                .Select(f => new FavoriteListItemDto
                {
                    FavoriteId = f.IntFavoriteId,
                    ProductId = f.IntProductId,
                    Added = f.DtmDateAdded
                })
                .ToListAsync();

            return Ok(new FavoritesListResponseDto
            {
                UserId = owner.UserId,
                Count = favorites.Count,
                Favorites = favorites
            });
        }

        /// <summary>
        /// Add a product to favorites for the authenticated user.
        /// </summary>
        [HttpPost("{productId:int}")]
        [Authorize]
        public async Task<IActionResult> AddFavorite(int productId)
        {
            var owner = await _identity.ResolveAsync(
                User,
                Request,
                Response,
                allowCreateGuestSession: false,
                allowAuthenticatedGuestSession: false
            );

            if (string.IsNullOrWhiteSpace(owner.UserId))
                return Unauthorized(new { error = "Missing NameIdentifier claim." });

            // Validate product exists
            var exists = await _db.Tproducts.AnyAsync(p => p.IntProductId == productId);
            if (!exists)
                return NotFound(new { error = "Product not found.", productId });

            // Prevent duplicates
            var already = await _db.Tfavorites.AnyAsync(f => f.UserId == owner.UserId && f.IntProductId == productId);
            if (already)
                return Ok(new FavoriteStatusResponseDto { Message = "Already favorited.", ProductId = productId });

            var fav = new Sobee.Domain.Entities.Reviews.Tfavorite
            {
                IntProductId = productId,
                DtmDateAdded = DateTime.UtcNow,
                UserId = owner.UserId
            };

            _db.Tfavorites.Add(fav);
            await _db.SaveChangesAsync();

            var response = new FavoriteAddedResponseDto
            {
                Message = "Favorited.",
                FavoriteId = fav.IntFavoriteId,
                ProductId = fav.IntProductId,
                Added = fav.DtmDateAdded
            };

            return StatusCode(StatusCodes.Status201Created, response);
        }

        /// <summary>
        /// Remove a product from favorites for the authenticated user.
        /// </summary>
        [HttpDelete("{productId:int}")]
        [Authorize]
        public async Task<IActionResult> RemoveFavorite(int productId)
        {
            var owner = await _identity.ResolveAsync(
                User,
                Request,
                Response,
                allowCreateGuestSession: false,
                allowAuthenticatedGuestSession: false
            );

            if (string.IsNullOrWhiteSpace(owner.UserId))
                return Unauthorized(new { error = "Missing NameIdentifier claim." });

            var fav = await _db.Tfavorites
                .FirstOrDefaultAsync(f => f.UserId == owner.UserId && f.IntProductId == productId);

            if (fav == null)
                return NotFound(new { error = "Favorite not found.", productId });

            _db.Tfavorites.Remove(fav);
            await _db.SaveChangesAsync();

            return Ok(new FavoriteStatusResponseDto { Message = "Unfavorited.", ProductId = productId });
        }
    }
}
