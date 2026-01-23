using sobee_API.Domain;
using sobee_API.DTOs.Admin;
using sobee_API.DTOs.Cart;
using sobee_API.DTOs.Common;

namespace sobee_API.Services.Interfaces;

public interface IAdminPromoService
{
    Task<ServiceResult<PagedResponse<AdminPromoResponse>>> GetPromosAsync(
        string? search,
        bool includeExpired,
        int page,
        int pageSize);
    Task<ServiceResult<AdminPromoResponse>> CreatePromoAsync(CreatePromoRequest request);
    Task<ServiceResult<AdminPromoResponse>> UpdatePromoAsync(int promoId, UpdatePromoRequest request);
    Task<ServiceResult<MessageResponseDto>> DeletePromoAsync(int promoId);
}
