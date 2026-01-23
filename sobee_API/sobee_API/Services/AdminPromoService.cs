using System.Globalization;
using sobee_API.Domain;
using sobee_API.DTOs.Admin;
using sobee_API.DTOs.Cart;
using sobee_API.DTOs.Common;
using sobee_API.Services.Interfaces;
using Sobee.Domain.Entities.Promotions;
using Sobee.Domain.Repositories;

namespace sobee_API.Services;

public sealed class AdminPromoService : IAdminPromoService
{
    private readonly IAdminPromoRepository _promoRepository;

    public AdminPromoService(IAdminPromoRepository promoRepository)
    {
        _promoRepository = promoRepository;
    }

    public async Task<ServiceResult<PagedResponse<AdminPromoResponse>>> GetPromosAsync(
        string? search,
        bool includeExpired,
        int page,
        int pageSize)
    {
        if (page <= 0)
        {
            return Validation<PagedResponse<AdminPromoResponse>>("page must be >= 1", new { page });
        }

        if (pageSize <= 0 || pageSize > 100)
        {
            return Validation<PagedResponse<AdminPromoResponse>>("pageSize must be between 1 and 100", new { pageSize });
        }

        var (promos, totalCount) = await _promoRepository.GetPromosAsync(search, includeExpired, page, pageSize);
        var codes = promos.Select(p => p.StrPromoCode).Where(code => !string.IsNullOrWhiteSpace(code)).ToList();
        var usageCounts = await _promoRepository.GetUsageCountsAsync(codes);
        var usageLookup = usageCounts.ToDictionary(x => x.Code, x => x.Count, StringComparer.OrdinalIgnoreCase);

        var items = promos.Select(p => ToPromoResponse(
            p.IntPromotionId,
            p.StrPromoCode,
            p.DecDiscountPercentage,
            p.DtmExpirationDate,
            usageLookup.TryGetValue(p.StrPromoCode, out var count) ? count : 0)).ToList();

        return ServiceResult<PagedResponse<AdminPromoResponse>>.Ok(new PagedResponse<AdminPromoResponse>
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Items = items
        });
    }

    public async Task<ServiceResult<AdminPromoResponse>> CreatePromoAsync(CreatePromoRequest request)
    {
        var validationError = ValidatePromoRequest(request.Code, request.DiscountPercentage, request.ExpirationDate);
        if (validationError != null)
        {
            return Validation<AdminPromoResponse>(validationError, null);
        }

        var code = NormalizeCode(request.Code);
        var exists = await _promoRepository.ExistsByCodeAsync(code);
        if (exists)
        {
            return Conflict<AdminPromoResponse>("Promo code already exists.", null);
        }

        var promo = new Tpromotion
        {
            StrPromoCode = code,
            DecDiscountPercentage = request.DiscountPercentage,
            StrDiscountPercentage = FormatDiscountLabel(request.DiscountPercentage),
            DtmExpirationDate = request.ExpirationDate
        };

        await _promoRepository.AddAsync(promo);
        await _promoRepository.SaveChangesAsync();

        return ServiceResult<AdminPromoResponse>.Ok(ToPromoResponse(
            promo.IntPromotionId,
            promo.StrPromoCode,
            promo.DecDiscountPercentage,
            promo.DtmExpirationDate,
            usageCount: 0));
    }

    public async Task<ServiceResult<AdminPromoResponse>> UpdatePromoAsync(int promoId, UpdatePromoRequest request)
    {
        var promo = await _promoRepository.FindByIdAsync(promoId, track: true);
        if (promo == null)
        {
            return NotFound<AdminPromoResponse>("Promo not found.", null);
        }

        if (request.Code != null)
        {
            var code = NormalizeCode(request.Code);
            if (string.IsNullOrWhiteSpace(code))
            {
                return Validation<AdminPromoResponse>("Promo code is required.", null);
            }

            var duplicate = await _promoRepository.ExistsByCodeAsync(code, promoId);
            if (duplicate)
            {
                return Conflict<AdminPromoResponse>("Promo code already exists.", null);
            }

            promo.StrPromoCode = code;
        }

        if (request.DiscountPercentage.HasValue)
        {
            var discount = request.DiscountPercentage.Value;
            if (discount <= 0 || discount > 100)
            {
                return Validation<AdminPromoResponse>("Discount percentage must be between 0 and 100.", null);
            }

            promo.DecDiscountPercentage = discount;
            promo.StrDiscountPercentage = FormatDiscountLabel(discount);
        }

        if (request.ExpirationDate.HasValue)
        {
            var expirationDate = request.ExpirationDate.Value;
            if (!IsValidExpirationDate(expirationDate))
            {
                return Validation<AdminPromoResponse>("Expiration date must be in the future.", null);
            }

            promo.DtmExpirationDate = expirationDate;
        }

        await _promoRepository.SaveChangesAsync();

        var usageCount = await _promoRepository.CountUsageAsync(promo.StrPromoCode);

        return ServiceResult<AdminPromoResponse>.Ok(ToPromoResponse(
            promo.IntPromotionId,
            promo.StrPromoCode,
            promo.DecDiscountPercentage,
            promo.DtmExpirationDate,
            usageCount));
    }

    public async Task<ServiceResult<MessageResponseDto>> DeletePromoAsync(int promoId)
    {
        var promo = await _promoRepository.FindByIdAsync(promoId, track: true);
        if (promo == null)
        {
            return NotFound<MessageResponseDto>("Promo not found.", null);
        }

        await _promoRepository.RemoveAsync(promo);
        await _promoRepository.SaveChangesAsync();

        return ServiceResult<MessageResponseDto>.Ok(new MessageResponseDto
        {
            Message = "Promo deleted."
        });
    }

    private static string? ValidatePromoRequest(string code, decimal discountPercentage, DateTime expirationDate)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return "Promo code is required.";
        }

        if (discountPercentage <= 0 || discountPercentage > 100)
        {
            return "Discount percentage must be between 0 and 100.";
        }

        if (!IsValidExpirationDate(expirationDate))
        {
            return "Expiration date must be in the future.";
        }

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

    private static ServiceResult<T> NotFound<T>(string message, object? data)
        => ServiceResult<T>.Fail("NotFound", message, data);

    private static ServiceResult<T> Validation<T>(string message, object? data)
        => ServiceResult<T>.Fail("ValidationError", message, data);

    private static ServiceResult<T> Conflict<T>(string message, object? data)
        => ServiceResult<T>.Fail("Conflict", message, data);
}
