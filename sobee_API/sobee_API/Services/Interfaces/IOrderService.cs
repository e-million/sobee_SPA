using sobee_API.Domain;
using sobee_API.DTOs.Orders;

namespace sobee_API.Services.Interfaces;

public interface IOrderService
{
    Task<ServiceResult<OrderResponse>> GetOrderAsync(string? userId, string? sessionId, int orderId);
    Task<ServiceResult<(IReadOnlyList<OrderResponse> Orders, int TotalCount)>> GetUserOrdersAsync(string userId, int page, int pageSize);
    Task<ServiceResult<OrderResponse>> CheckoutAsync(string? userId, string? sessionId, CheckoutRequest request);
    Task<ServiceResult<OrderResponse>> CancelOrderAsync(string? userId, string? sessionId, int orderId);
    Task<ServiceResult<OrderResponse>> PayOrderAsync(string? userId, string? sessionId, int orderId, PayOrderRequest request);
    Task<ServiceResult<OrderResponse>> UpdateStatusAsync(int orderId, UpdateOrderStatusRequest request);
}
