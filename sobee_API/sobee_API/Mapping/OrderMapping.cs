using System.Collections.Generic;
using System.Linq;
using sobee_API.DTOs.Orders;
using Sobee.Domain.Entities.Orders;

namespace sobee_API.Mapping;

public static class OrderMapping
{
    public static OrderResponse ToOrderResponse(this Torder order)
    {
        var ownerType = !string.IsNullOrWhiteSpace(order.UserId) ? "user" : "guest";

        return new OrderResponse
        {
            OrderId = order.IntOrderId,
            OrderDate = order.DtmOrderDate,
            TotalAmount = order.DecTotalAmount,
            OrderStatus = order.StrOrderStatus,
            OwnerType = ownerType,
            UserId = order.UserId,
            GuestSessionId = order.SessionId,
            ShippingAddress = order.StrShippingAddress,
            BillingAddress = order.StrBillingAddress,
            SubtotalAmount = order.DecSubtotalAmount,
            DiscountAmount = order.DecDiscountAmount,
            DiscountPercentage = order.DecDiscountPercentage,
            PromoCode = order.StrPromoCode,
            Items = order.TorderItems == null
                ? new List<OrderItemResponse>()
                : order.TorderItems.Select(ToOrderItemResponse).ToList()
        };
    }

    public static OrderItemResponse ToOrderItemResponse(this TorderItem item)
    {
        var quantity = item.IntQuantity ?? 0;
        var unitPrice = item.MonPricePerUnit ?? (item.IntProduct?.DecPrice ?? 0m);

        return new OrderItemResponse
        {
            OrderItemId = item.IntOrderItemId,
            ProductId = item.IntProductId,
            ProductName = item.IntProduct?.StrName,
            UnitPrice = unitPrice,
            Quantity = quantity,
            LineTotal = quantity * unitPrice
        };
    }
}
