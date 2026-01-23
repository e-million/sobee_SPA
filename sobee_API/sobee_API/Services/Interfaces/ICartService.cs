using sobee_API.Domain;
using sobee_API.DTOs;
using sobee_API.DTOs.Cart;

namespace sobee_API.Services.Interfaces;

public interface ICartService
{
    Task<ServiceResult<CartResponseDto>> GetCartAsync(string? userId, string? sessionId, bool canMergeGuestSession);
    Task<ServiceResult<CartResponseDto>> GetExistingCartAsync(string? userId, string? sessionId);
    Task<ServiceResult<CartResponseDto>> AddItemAsync(string? userId, string? sessionId, bool canMergeGuestSession, AddCartItemRequest request);
    Task<ServiceResult<CartResponseDto>> UpdateItemAsync(string? userId, string? sessionId, int cartItemId, UpdateCartItemRequest request);
    Task<ServiceResult<CartResponseDto>> RemoveItemAsync(string? userId, string? sessionId, int cartItemId);
    Task<ServiceResult<CartResponseDto>> ClearCartAsync(string? userId, string? sessionId);
    Task<ServiceResult<PromoAppliedResponseDto>> ApplyPromoAsync(string? userId, string? sessionId, string promoCode);
    Task<ServiceResult<MessageResponseDto>> RemovePromoAsync(string? userId, string? sessionId);
}
