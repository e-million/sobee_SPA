using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sobee.Domain.Data;
using Sobee.Domain.Entities.Promotions;
using sobee_API.DTOs.Admin;
using sobee_API.DTOs.Common;
using System.Globalization;

namespace sobee_API.Controllers
{
    [ApiController]
    [Route("api/admin/promos")]
    [Authorize(Roles = "Admin")]
    public class AdminPromosController : ControllerBase
    {
        private readonly SobeecoredbContext _db;

        public AdminPromosController(SobeecoredbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetPromos(
            [FromQuery] string? search,
            [FromQuery] bool includeExpired = false,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            if (page <= 0)
                return BadRequest(new ApiErrorResponse("page must be >= 1", "ValidationError"));

            if (pageSize <= 0 || pageSize > 100)
                return BadRequest(new ApiErrorResponse("pageSize must be between 1 and 100", "ValidationError"));

            var query = _db.Tpromotions.AsNoTracking();

            if (!includeExpired)
            {
                var today = DateTime.Today;
                query = query.Where(p => p.DtmExpirationDate >= today);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(p => p.StrPromoCode.Contains(term));
            }

            var totalCount = await query.CountAsync();

            var promos = await query
                .OrderByDescending(p => p.DtmExpirationDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new
                {
                    p.IntPromotionId,
                    p.StrPromoCode,
                    p.DecDiscountPercentage,
                    p.DtmExpirationDate
                })
                .ToListAsync();

            var codes = promos.Select(p => p.StrPromoCode).ToList();

            var usageCounts = await _db.Torders
                .Where(o => o.StrPromoCode != null && codes.Contains(o.StrPromoCode))
                .GroupBy(o => o.StrPromoCode)
                .Select(g => new { Code = g.Key!, Count = g.Count() })
                .ToListAsync();

            var usageLookup = usageCounts.ToDictionary(x => x.Code, x => x.Count, StringComparer.OrdinalIgnoreCase);

            var items = promos.Select(p => ToPromoResponse(
                p.IntPromotionId,
                p.StrPromoCode,
                p.DecDiscountPercentage,
                p.DtmExpirationDate,
                usageLookup.TryGetValue(p.StrPromoCode, out var count) ? count : 0)).ToList();

            return Ok(new
            {
                page,
                pageSize,
                totalCount,
                items
            });
        }

        [HttpPost]
        public async Task<IActionResult> CreatePromo([FromBody] CreatePromoRequest request)
        {
            var validationError = ValidatePromoRequest(request.Code, request.DiscountPercentage, request.ExpirationDate);
            if (validationError != null)
                return BadRequest(new ApiErrorResponse(validationError, "ValidationError"));

            var code = NormalizeCode(request.Code);

            var exists = await _db.Tpromotions.AnyAsync(p => p.StrPromoCode == code);
            if (exists)
                return Conflict(new ApiErrorResponse("Promo code already exists.", "Conflict"));

            var promo = new Tpromotion
            {
                StrPromoCode = code,
                DecDiscountPercentage = request.DiscountPercentage,
                StrDiscountPercentage = FormatDiscountLabel(request.DiscountPercentage),
                DtmExpirationDate = request.ExpirationDate
            };

            _db.Tpromotions.Add(promo);
            await _db.SaveChangesAsync();

            return Ok(ToPromoResponse(promo.IntPromotionId, promo.StrPromoCode, promo.DecDiscountPercentage, promo.DtmExpirationDate, 0));
        }

        [HttpPut("{promoId:int}")]
        public async Task<IActionResult> UpdatePromo(int promoId, [FromBody] UpdatePromoRequest request)
        {
            var promo = await _db.Tpromotions.FirstOrDefaultAsync(p => p.IntPromotionId == promoId);
            if (promo == null)
                return NotFound(new ApiErrorResponse("Promo not found.", "NotFound"));

            if (request.Code != null)
            {
                var code = NormalizeCode(request.Code);
                if (string.IsNullOrWhiteSpace(code))
                    return BadRequest(new ApiErrorResponse("Promo code is required.", "ValidationError"));

                var duplicate = await _db.Tpromotions.AnyAsync(p => p.IntPromotionId != promoId && p.StrPromoCode == code);
                if (duplicate)
                    return Conflict(new ApiErrorResponse("Promo code already exists.", "Conflict"));

                promo.StrPromoCode = code;
            }

            if (request.DiscountPercentage.HasValue)
            {
                var discount = request.DiscountPercentage.Value;
                if (discount <= 0 || discount > 100)
                    return BadRequest(new ApiErrorResponse("Discount percentage must be between 0 and 100.", "ValidationError"));

                promo.DecDiscountPercentage = discount;
                promo.StrDiscountPercentage = FormatDiscountLabel(discount);
            }

            if (request.ExpirationDate.HasValue)
            {
                var expirationDate = request.ExpirationDate.Value;
                if (!IsValidExpirationDate(expirationDate))
                    return BadRequest(new ApiErrorResponse("Expiration date must be in the future.", "ValidationError"));

                promo.DtmExpirationDate = expirationDate;
            }

            await _db.SaveChangesAsync();

            var usageCount = await _db.Torders.CountAsync(o => o.StrPromoCode == promo.StrPromoCode);

            return Ok(ToPromoResponse(promo.IntPromotionId, promo.StrPromoCode, promo.DecDiscountPercentage, promo.DtmExpirationDate, usageCount));
        }

        [HttpDelete("{promoId:int}")]
        public async Task<IActionResult> DeletePromo(int promoId)
        {
            var promo = await _db.Tpromotions.FirstOrDefaultAsync(p => p.IntPromotionId == promoId);
            if (promo == null)
                return NotFound(new ApiErrorResponse("Promo not found.", "NotFound"));

            _db.Tpromotions.Remove(promo);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Promo deleted." });
        }

        private static string? ValidatePromoRequest(string code, decimal discountPercentage, DateTime expirationDate)
        {
            if (string.IsNullOrWhiteSpace(code))
                return "Promo code is required.";

            if (discountPercentage <= 0 || discountPercentage > 100)
                return "Discount percentage must be between 0 and 100.";

            if (!IsValidExpirationDate(expirationDate))
                return "Expiration date must be in the future.";

            return null;
        }

        private static bool IsValidExpirationDate(DateTime expirationDate)
            => expirationDate.Date > DateTime.Today;

        private static string NormalizeCode(string code)
            => code.Trim().ToUpperInvariant();

        private static string FormatDiscountLabel(decimal discountPercentage)
            => string.Format(CultureInfo.InvariantCulture, "{0:0.##}%", discountPercentage);

        private static AdminPromoResponse ToPromoResponse(
            int id,
            string code,
            decimal discountPercentage,
            DateTime expirationDate,
            int usageCount)
        {
            return new AdminPromoResponse
            {
                Id = id,
                Code = code,
                DiscountPercentage = discountPercentage,
                ExpirationDate = expirationDate,
                UsageCount = usageCount,
                IsExpired = expirationDate.Date < DateTime.Today
            };
        }
    }
}
