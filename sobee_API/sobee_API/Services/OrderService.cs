using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Options;
using sobee_API.Constants;
using sobee_API.Configuration;
using sobee_API.Domain;
using sobee_API.DTOs.Orders;
using sobee_API.Mapping;
using sobee_API.Services.Interfaces;
using Sobee.Domain.Entities.Orders;
using Sobee.Domain.Entities.Payments;
using Sobee.Domain.Repositories;

namespace sobee_API.Services;

public sealed class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly ICartService _cartService;
    private readonly IInventoryService _inventoryService;
    private readonly TaxSettings _taxSettings;

    public OrderService(
        IOrderRepository orderRepository,
        IPaymentRepository paymentRepository,
        ICartService cartService,
        IInventoryService inventoryService,
        IOptions<TaxSettings> taxSettings)
    {
        _orderRepository = orderRepository;
        _paymentRepository = paymentRepository;
        _cartService = cartService;
        _inventoryService = inventoryService;
        _taxSettings = taxSettings.Value;
    }

    public async Task<ServiceResult<OrderResponse>> GetOrderAsync(string? userId, string? sessionId, int orderId)
    {
        var order = await _orderRepository.FindForOwnerWithItemsAsync(orderId, userId, sessionId, track: false);
        if (order == null)
        {
            return NotFound<OrderResponse>("Order not found.", new { orderId });
        }

        return ServiceResult<OrderResponse>.Ok(order.ToOrderResponse());
    }

    public async Task<ServiceResult<(IReadOnlyList<OrderResponse> Orders, int TotalCount)>> GetUserOrdersAsync(
        string userId,
        int page,
        int pageSize)
    {
        if (page <= 0)
        {
            return Validation<(IReadOnlyList<OrderResponse>, int)>("page must be >= 1", new { page });
        }

        if (pageSize <= 0 || pageSize > 100)
        {
            return Validation<(IReadOnlyList<OrderResponse>, int)>("pageSize must be between 1 and 100", new { pageSize });
        }

        var totalCount = await _orderRepository.CountUserOrdersAsync(userId);
        var orders = await _orderRepository.GetUserOrdersAsync(userId, page, pageSize);
        var mapped = orders.Select(order => order.ToOrderResponse()).ToList();

        return ServiceResult<(IReadOnlyList<OrderResponse>, int)>.Ok((mapped, totalCount));
    }

    public async Task<ServiceResult<OrderResponse>> CheckoutAsync(string? userId, string? sessionId, CheckoutRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ShippingAddress))
        {
            return Validation<OrderResponse>("Shipping address is required.", null);
        }

        if (string.IsNullOrWhiteSpace(request.BillingAddress))
        {
            return Validation<OrderResponse>("Billing address is required.", null);
        }

        var cartResult = await _cartService.GetExistingCartAsync(userId, sessionId);
        if (!cartResult.Success)
        {
            return ServiceResult<OrderResponse>.Fail(
                cartResult.ErrorCode ?? "ValidationError",
                cartResult.ErrorMessage ?? "No cart found for this owner.",
                cartResult.ErrorData);
        }

        var cart = cartResult.Value!;
        if (cart.Items.Count == 0)
        {
            return Validation<OrderResponse>("Cart is empty.", null);
        }

        var lineItems = new List<CartLineItem>();
        var inventoryItems = new List<InventoryRequestItem>();

        foreach (var item in cart.Items)
        {
            var quantity = item.Quantity ?? 0;

            if (quantity <= 0)
            {
                return Validation<OrderResponse>(
                    "Cart has an item with invalid quantity.",
                    new { cartItemId = item.CartItemId });
            }

            if (item.Product == null || item.ProductId == null)
            {
                return Validation<OrderResponse>(
                    "Cart contains an item with missing product reference.",
                    new { cartItemId = item.CartItemId });
            }

            lineItems.Add(new CartLineItem(quantity, item.Product.Price));
            inventoryItems.Add(new InventoryRequestItem(item.ProductId.Value, quantity));
        }

        var subtotal = CartCalculator.CalculateSubtotal(lineItems);
        var discount = cart.Promo == null
            ? 0m
            : PromoCalculator.CalculateDiscount(subtotal, cart.Promo.DiscountPercentage);
        var taxRate = _taxSettings.TaxEnabled ? _taxSettings.DefaultTaxRate : 0m;
        var taxableAmount = CartCalculator.CalculateTotal(subtotal, discount);
        var tax = TaxCalculator.CalculateTax(taxableAmount, taxRate);
        var total = taxableAmount + tax;

        using var tx = await _orderRepository.BeginTransactionAsync();

        try
        {
            var inventoryResult = await _inventoryService.ValidateAndDecrementAsync(inventoryItems);
            if (!inventoryResult.Success)
            {
                await tx.RollbackAsync();
                return ServiceResult<OrderResponse>.Fail(
                    inventoryResult.ErrorCode ?? "Conflict",
                    inventoryResult.ErrorMessage ?? "Insufficient stock.",
                    inventoryResult.ErrorData);
            }

            var order = new Torder
            {
                DtmOrderDate = DateTime.UtcNow,
                DecSubtotalAmount = subtotal,
                DecDiscountPercentage = cart.Promo?.DiscountPercentage,
                DecDiscountAmount = discount,
                StrPromoCode = cart.Promo?.Code,
                DecTotalAmount = total,
                DecTaxAmount = tax,
                DecTaxRate = taxRate,
                StrShippingAddress = request.ShippingAddress,
                StrBillingAddress = request.BillingAddress,
                IntPaymentMethodId = request.PaymentMethodId,
                StrOrderStatus = OrderStatuses.Pending,
                UserId = userId,
                SessionId = sessionId
            };

            await _orderRepository.AddAsync(order);
            await _orderRepository.SaveChangesAsync();

            var orderItems = cart.Items.Select(item =>
            {
                var quantity = item.Quantity ?? 0;
                var unitPrice = item.Product?.Price ?? 0m;
                return new TorderItem
                {
                    IntOrderId = order.IntOrderId,
                    IntProductId = item.ProductId,
                    IntQuantity = quantity,
                    MonPricePerUnit = unitPrice
                };
            }).ToList();

            await _orderRepository.AddItemsAsync(orderItems);

            var clearResult = await _cartService.ClearCartAsync(userId, sessionId);
            if (!clearResult.Success)
            {
                await tx.RollbackAsync();
                return ServiceResult<OrderResponse>.Fail(
                    clearResult.ErrorCode ?? "ServerError",
                    clearResult.ErrorMessage ?? "Failed to clear cart.",
                    clearResult.ErrorData);
            }

            if (cart.Promo != null)
            {
                var removePromoResult = await _cartService.RemovePromoAsync(userId, sessionId);
                if (!removePromoResult.Success && removePromoResult.ErrorCode != "ValidationError")
                {
                    await tx.RollbackAsync();
                    return ServiceResult<OrderResponse>.Fail(
                        removePromoResult.ErrorCode ?? "ServerError",
                        removePromoResult.ErrorMessage ?? "Failed to remove promo.",
                        removePromoResult.ErrorData);
                }
            }

            await _orderRepository.SaveChangesAsync();
            await tx.CommitAsync();

            var created = await _orderRepository.FindByIdWithItemsAsync(order.IntOrderId, track: false);
            if (created == null)
            {
                return ServiceResult<OrderResponse>.Fail("ServerError", "Checkout failed.", null);
            }

            return ServiceResult<OrderResponse>.Ok(created.ToOrderResponse());
        }
        catch
        {
            await tx.RollbackAsync();
            return ServiceResult<OrderResponse>.Fail("ServerError", "Checkout failed.", null);
        }
    }

    public async Task<ServiceResult<OrderResponse>> CancelOrderAsync(string? userId, string? sessionId, int orderId)
    {
        var order = await _orderRepository.FindForOwnerWithItemsAsync(orderId, userId, sessionId, track: true);
        if (order == null)
        {
            return NotFound<OrderResponse>("Order not found.", new { orderId });
        }

        var currentStatus = string.IsNullOrWhiteSpace(order.StrOrderStatus)
            ? OrderStatuses.Pending
            : OrderStatuses.Normalize(order.StrOrderStatus);

        if (!OrderStatusMachine.IsCancellable(currentStatus))
        {
            return Conflict<OrderResponse>(
                "Order cannot be cancelled in its current status.",
                new { status = currentStatus });
        }

        order.StrOrderStatus = OrderStatuses.Cancelled;
        await _orderRepository.SaveChangesAsync();

        return ServiceResult<OrderResponse>.Ok(order.ToOrderResponse());
    }

    public async Task<ServiceResult<OrderResponse>> PayOrderAsync(
        string? userId,
        string? sessionId,
        int orderId,
        PayOrderRequest request)
    {
        var order = await _orderRepository.FindForOwnerAsync(orderId, userId, sessionId, track: true);
        if (order == null)
        {
            return NotFound<OrderResponse>("Order not found.", new { orderId });
        }

        var paymentMethod = await _paymentRepository.FindMethodAsync(request.PaymentMethodId);
        if (paymentMethod == null)
        {
            return NotFound<OrderResponse>(
                $"Payment method {request.PaymentMethodId} not found.",
                new { paymentMethodId = request.PaymentMethodId });
        }

        var currentStatus = string.IsNullOrWhiteSpace(order.StrOrderStatus)
            ? OrderStatuses.Pending
            : OrderStatuses.Normalize(order.StrOrderStatus);
        var targetStatus = OrderStatuses.Paid;

        if (!OrderStatusMachine.CanTransition(currentStatus, targetStatus))
        {
            return Conflict<OrderResponse>(
                "Invalid status transition.",
                new { orderId, fromStatus = order.StrOrderStatus, toStatus = targetStatus });
        }

        using var tx = await _orderRepository.BeginTransactionAsync();

        try
        {
            var payment = new Tpayment
            {
                IntPaymentMethodId = paymentMethod.IntPaymentMethodId,
                StrBillingAddress = paymentMethod.StrBillingAddress
            };

            await _paymentRepository.AddAsync(payment);
            order.IntPaymentMethodId = paymentMethod.IntPaymentMethodId;
            order.StrOrderStatus = targetStatus;

            await _orderRepository.SaveChangesAsync();
            await tx.CommitAsync();

            var updated = await _orderRepository.FindByIdWithItemsAsync(order.IntOrderId, track: false);
            if (updated == null)
            {
                return ServiceResult<OrderResponse>.Fail("ServerError", "Payment failed.", null);
            }

            return ServiceResult<OrderResponse>.Ok(updated.ToOrderResponse());
        }
        catch
        {
            await tx.RollbackAsync();
            return ServiceResult<OrderResponse>.Fail("ServerError", "Payment failed.", null);
        }
    }

    public async Task<ServiceResult<OrderResponse>> UpdateStatusAsync(int orderId, UpdateOrderStatusRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Status))
        {
            return Validation<OrderResponse>("Status is required.", null);
        }

        var newStatus = OrderStatuses.Normalize(request.Status);
        var order = await _orderRepository.FindByIdWithItemsAsync(orderId, track: true);
        if (order == null)
        {
            return NotFound<OrderResponse>("Order not found.", new { orderId });
        }

        var currentStatus = string.IsNullOrWhiteSpace(order.StrOrderStatus)
            ? OrderStatuses.Pending
            : OrderStatuses.Normalize(order.StrOrderStatus);

        if (!OrderStatusMachine.CanTransition(currentStatus, newStatus))
        {
            return Conflict<OrderResponse>(
                "Invalid status transition.",
                new { orderId, fromStatus = order.StrOrderStatus, toStatus = request.Status });
        }

        order.StrOrderStatus = newStatus;
        if (string.Equals(newStatus, OrderStatuses.Shipped, StringComparison.OrdinalIgnoreCase)
            && order.DtmShippedDate == null)
        {
            order.DtmShippedDate = DateTime.UtcNow;
        }

        if (string.Equals(newStatus, OrderStatuses.Delivered, StringComparison.OrdinalIgnoreCase)
            && order.DtmDeliveredDate == null)
        {
            order.DtmDeliveredDate = DateTime.UtcNow;
            if (order.DtmShippedDate == null)
            {
                order.DtmShippedDate = order.DtmDeliveredDate;
            }
        }

        await _orderRepository.SaveChangesAsync();

        return ServiceResult<OrderResponse>.Ok(order.ToOrderResponse());
    }

    private static ServiceResult<T> NotFound<T>(string message, object? data)
        => ServiceResult<T>.Fail("NotFound", message, data);

    private static ServiceResult<T> Validation<T>(string message, object? data)
        => ServiceResult<T>.Fail("ValidationError", message, data);

    private static ServiceResult<T> Conflict<T>(string message, object? data)
        => ServiceResult<T>.Fail("InvalidStatusTransition", message, data);
}
