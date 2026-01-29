using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using sobee_API.Constants;
using sobee_API.Configuration;
using sobee_API.Domain;
using sobee_API.DTOs.Cart;
using sobee_API.DTOs.Common;
using sobee_API.DTOs.Orders;
using sobee_API.Services;
using sobee_API.Services.Interfaces;
using Sobee.Domain.Entities.Orders;
using Sobee.Domain.Entities.Payments;
using Sobee.Domain.Repositories;
using Xunit;

namespace sobee_API.Tests.Services;

public class OrderServiceTests
{
    [Fact]
    public async Task GetOrderAsync_NotFound_ReturnsError()
    {
        using var context = new TestContext();

        var result = await context.Service.GetOrderAsync("user-1", null, 999);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("NotFound");
    }

    [Fact]
    public async Task GetOrderAsync_Owner_ReturnsOrder()
    {
        using var context = new TestContext();
        var order = context.OrderRepository.AddOrder(CreateOrder(userId: "user-1"));

        var result = await context.Service.GetOrderAsync("user-1", null, order.IntOrderId);

        result.Success.Should().BeTrue();
        result.Value!.OrderId.Should().Be(order.IntOrderId);
    }

    [Fact]
    public async Task GetUserOrdersAsync_ReturnsPagedResults()
    {
        using var context = new TestContext();
        context.OrderRepository.AddOrder(CreateOrder(userId: "user-1", orderDate: DateTime.UtcNow.AddDays(-2)));
        context.OrderRepository.AddOrder(CreateOrder(userId: "user-1", orderDate: DateTime.UtcNow.AddDays(-1)));
        context.OrderRepository.AddOrder(CreateOrder(userId: "user-1", orderDate: DateTime.UtcNow));

        var result = await context.Service.GetUserOrdersAsync("user-1", page: 1, pageSize: 2);

        result.Success.Should().BeTrue();
        result.Value.TotalCount.Should().Be(3);
        result.Value.Orders.Should().HaveCount(2);
        result.Value.Orders[0].OrderDate.Should().BeAfter((DateTime)result.Value.Orders[1].OrderDate);
    }

    [Fact]
    public async Task CheckoutAsync_EmptyCart_ReturnsValidationError()
    {
        using var context = new TestContext();
        context.CartService.SetCart(CreateCartResponse(items: new List<CartItemResponseDto>()));

        var result = await context.Service.CheckoutAsync(null, "session-1", CreateCheckoutRequest());

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("ValidationError");
    }

    [Fact]
    public async Task CheckoutAsync_InsufficientStock_ReturnsConflict()
    {
        using var context = new TestContext();
        context.CartService.SetCart(CreateCartResponse(items: new List<CartItemResponseDto>
        {
            CreateCartItem(1, 2, 5m)
        }));
        context.InventoryService.SetResult(ServiceResult<bool>.Fail(
            "InsufficientStock",
            "Insufficient stock.",
            new { productId = 1 }));

        var result = await context.Service.CheckoutAsync(null, "session-1", CreateCheckoutRequest());

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("InsufficientStock");
    }

    [Fact]
    public async Task CheckoutAsync_Success_CreatesOrder_AndClearsCart()
    {
        using var context = new TestContext();
        context.CartService.SetCart(CreateCartResponse(items: new List<CartItemResponseDto>
        {
            CreateCartItem(1, 2, 5m),
            CreateCartItem(2, 1, 6m)
        }, promo: new CartPromoDto { Code = "SAVE10", DiscountPercentage = 10m }));
        context.InventoryService.SetResult(ServiceResult<bool>.Ok(true));

        var result = await context.Service.CheckoutAsync("user-1", "session-1", CreateCheckoutRequest());

        result.Success.Should().BeTrue();
        context.CartService.ClearCalled.Should().BeTrue();
        context.CartService.RemovePromoCalled.Should().BeTrue();
        context.OrderRepository.Orders.Should().HaveCount(1);
        context.OrderRepository.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task CancelOrderAsync_InvalidStatus_ReturnsConflict()
    {
        using var context = new TestContext();
        var order = context.OrderRepository.AddOrder(CreateOrder(userId: "user-1", status: OrderStatuses.Shipped));

        var result = await context.Service.CancelOrderAsync("user-1", null, order.IntOrderId);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("InvalidStatusTransition");
    }

    [Fact]
    public async Task PayOrderAsync_InvalidMethod_ReturnsNotFound()
    {
        using var context = new TestContext();
        var order = context.OrderRepository.AddOrder(CreateOrder(userId: "user-1"));

        var result = await context.Service.PayOrderAsync("user-1", null, order.IntOrderId, new PayOrderRequest { PaymentMethodId = 999 });

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("NotFound");
    }

    [Fact]
    public async Task PayOrderAsync_Valid_TransitionsToPaid()
    {
        using var context = new TestContext();
        var order = context.OrderRepository.AddOrder(CreateOrder(userId: "user-1", status: OrderStatuses.Pending));
        context.PaymentRepository.AddMethod(CreatePaymentMethod(1));

        var result = await context.Service.PayOrderAsync("user-1", null, order.IntOrderId, new PayOrderRequest { PaymentMethodId = 1 });

        result.Success.Should().BeTrue();
        result.Value!.OrderStatus.Should().Be(OrderStatuses.Paid);
    }

    [Fact]
    public async Task UpdateStatusAsync_Valid_SetsShippedTimestamp()
    {
        using var context = new TestContext();
        var order = context.OrderRepository.AddOrder(CreateOrder(status: OrderStatuses.Processing));

        var result = await context.Service.UpdateStatusAsync(order.IntOrderId, new UpdateOrderStatusRequest { Status = "Shipped" });

        result.Success.Should().BeTrue();
        context.OrderRepository.Orders.First().DtmShippedDate.Should().NotBeNull();
    }

    private static CheckoutRequest CreateCheckoutRequest()
        => new()
        {
            ShippingAddress = "123 Test Lane",
            BillingAddress = "123 Test Lane",
            PaymentMethodId = 1
        };

    private static Torder CreateOrder(string? userId = null, string? sessionId = null, string? status = null, DateTime? orderDate = null)
        => new()
        {
            UserId = userId,
            SessionId = sessionId,
            StrOrderStatus = status ?? OrderStatuses.Pending,
            DtmOrderDate = orderDate ?? DateTime.UtcNow,
            DecTotalAmount = 10m
        };

    private static TpaymentMethod CreatePaymentMethod(int id)
        => new()
        {
            IntPaymentMethodId = id,
            StrBillingAddress = "123 Test Lane",
            StrCreditCardDetails = "****",
            StrDescription = "Test"
        };

    private static CartResponseDto CreateCartResponse(IReadOnlyList<CartItemResponseDto> items, CartPromoDto? promo = null)
        => new()
        {
            CartId = 1,
            Owner = "guest",
            Items = items.ToList(),
            Promo = promo
        };

    private static CartItemResponseDto CreateCartItem(int productId, int quantity, decimal price)
        => new()
        {
            CartItemId = productId * 10,
            ProductId = productId,
            Quantity = quantity,
            Product = new CartProductDto
            {
                Id = productId,
                Name = $"Product-{productId}",
                Price = price
            },
            LineTotal = quantity * price
        };

    private sealed class TestContext : IDisposable
    {
        public FakeOrderRepository OrderRepository { get; }
        public FakePaymentRepository PaymentRepository { get; }
        public FakeCartService CartService { get; }
        public FakeInventoryService InventoryService { get; }
        public OrderService Service { get; }

        public TestContext()
        {
            OrderRepository = new FakeOrderRepository();
            PaymentRepository = new FakePaymentRepository();
            CartService = new FakeCartService();
            InventoryService = new FakeInventoryService();
            Service = new OrderService(
                OrderRepository,
                PaymentRepository,
                CartService,
                InventoryService,
                Options.Create(new TaxSettings()));
        }

        public void Dispose()
        {
        }
    }

    private sealed class FakeOrderRepository : IOrderRepository
    {
        private int _nextOrderId = 1;
        private int _nextOrderItemId = 1;
        private readonly List<Torder> _orders = new();
        private readonly List<TorderItem> _items = new();

        public IReadOnlyList<Torder> Orders => _orders;
        public IReadOnlyList<TorderItem> Items => _items;

        public Torder AddOrder(Torder order)
        {
            if (order.IntOrderId == 0)
            {
                order.IntOrderId = _nextOrderId++;
            }

            _orders.Add(order);
            return order;
        }

        public Task<Torder?> FindByIdAsync(int orderId, bool track = true)
        {
            var order = _orders.FirstOrDefault(o => o.IntOrderId == orderId);
            AttachItems(order);
            return Task.FromResult(order);
        }

        public Task<Torder?> FindByIdWithItemsAsync(int orderId, bool track = true)
        {
            var order = _orders.FirstOrDefault(o => o.IntOrderId == orderId);
            AttachItems(order);
            return Task.FromResult(order);
        }

        public Task<Torder?> FindForOwnerAsync(int orderId, string? userId, string? sessionId, bool track = true)
        {
            var order = FindOwner(orderId, userId, sessionId);
            AttachItems(order);
            return Task.FromResult(order);
        }

        public Task<Torder?> FindForOwnerWithItemsAsync(int orderId, string? userId, string? sessionId, bool track = true)
        {
            var order = FindOwner(orderId, userId, sessionId);
            AttachItems(order);
            return Task.FromResult(order);
        }

        public Task<IReadOnlyList<Torder>> GetUserOrdersAsync(string userId, int page, int pageSize)
        {
            IReadOnlyList<Torder> orders = _orders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.DtmOrderDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            foreach (var order in orders)
            {
                AttachItems(order);
            }

            return Task.FromResult(orders);
        }

        public Task<int> CountUserOrdersAsync(string userId)
            => Task.FromResult(_orders.Count(o => o.UserId == userId));

        public Task AddAsync(Torder order)
        {
            AddOrder(order);
            return Task.CompletedTask;
        }

        public Task AddItemsAsync(IEnumerable<TorderItem> items)
        {
            foreach (var item in items)
            {
                if (item.IntOrderItemId == 0)
                {
                    item.IntOrderItemId = _nextOrderItemId++;
                }

                _items.Add(item);
            }

            return Task.CompletedTask;
        }

        public Task SaveChangesAsync()
            => Task.CompletedTask;

        public Task<IDbContextTransaction> BeginTransactionAsync()
            => Task.FromResult<IDbContextTransaction>(new FakeDbTransaction());

        private Torder? FindOwner(int orderId, string? userId, string? sessionId)
        {
            return _orders.FirstOrDefault(o =>
                o.IntOrderId == orderId &&
                (!string.IsNullOrWhiteSpace(userId)
                    ? o.UserId == userId
                    : o.SessionId == sessionId));
        }

        private void AttachItems(Torder? order)
        {
            if (order == null)
            {
                return;
            }

            order.TorderItems = _items.Where(i => i.IntOrderId == order.IntOrderId).ToList();
        }
    }

    private sealed class FakePaymentRepository : IPaymentRepository
    {
        private readonly List<TpaymentMethod> _methods = new();
        private readonly List<Tpayment> _payments = new();

        public void AddMethod(TpaymentMethod method)
        {
            _methods.Add(method);
        }

        public Task<TpaymentMethod?> FindMethodAsync(int paymentMethodId)
        {
            var method = _methods.FirstOrDefault(m => m.IntPaymentMethodId == paymentMethodId);
            return Task.FromResult(method);
        }

        public Task<IReadOnlyList<TpaymentMethod>> GetMethodsAsync()
        {
            IReadOnlyList<TpaymentMethod> methods = _methods.ToList();
            return Task.FromResult(methods);
        }

        public Task AddAsync(Tpayment payment)
        {
            _payments.Add(payment);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync()
            => Task.CompletedTask;
    }

    private sealed class FakeCartService : ICartService
    {
        private CartResponseDto? _cart;

        public bool ClearCalled { get; private set; }
        public bool RemovePromoCalled { get; private set; }

        public void SetCart(CartResponseDto cart)
        {
            _cart = cart;
        }

        public Task<ServiceResult<CartResponseDto>> GetCartAsync(string? userId, string? sessionId, bool canMergeGuestSession)
            => throw new NotImplementedException();

        public Task<ServiceResult<CartResponseDto>> GetExistingCartAsync(string? userId, string? sessionId)
        {
            if (_cart == null)
            {
                return Task.FromResult(ServiceResult<CartResponseDto>.Fail("ValidationError", "No cart found for this owner."));
            }

            return Task.FromResult(ServiceResult<CartResponseDto>.Ok(_cart));
        }

        public Task<ServiceResult<CartResponseDto>> AddItemAsync(string? userId, string? sessionId, bool canMergeGuestSession, AddCartItemRequest request)
            => throw new NotImplementedException();

        public Task<ServiceResult<CartResponseDto>> UpdateItemAsync(string? userId, string? sessionId, int cartItemId, UpdateCartItemRequest request)
            => throw new NotImplementedException();

        public Task<ServiceResult<CartResponseDto>> RemoveItemAsync(string? userId, string? sessionId, int cartItemId)
            => throw new NotImplementedException();

        public Task<ServiceResult<CartResponseDto>> ClearCartAsync(string? userId, string? sessionId)
        {
            ClearCalled = true;
            return Task.FromResult(ServiceResult<CartResponseDto>.Ok(_cart ?? new CartResponseDto()));
        }

        public Task<ServiceResult<PromoAppliedResponseDto>> ApplyPromoAsync(string? userId, string? sessionId, string promoCode)
            => throw new NotImplementedException();

        public Task<ServiceResult<MessageResponseDto>> RemovePromoAsync(string? userId, string? sessionId)
        {
            RemovePromoCalled = true;
            return Task.FromResult(ServiceResult<MessageResponseDto>.Ok(new MessageResponseDto { Message = "Promo code removed." }));
        }
    }

    private sealed class FakeInventoryService : IInventoryService
    {
        private ServiceResult<bool> _result = ServiceResult<bool>.Ok(true);

        public void SetResult(ServiceResult<bool> result)
        {
            _result = result;
        }

        public Task<ServiceResult<bool>> ValidateAndDecrementAsync(IReadOnlyList<InventoryRequestItem> items)
            => Task.FromResult(_result);
    }

    private sealed class FakeDbTransaction : IDbContextTransaction
    {
        public Guid TransactionId { get; } = Guid.NewGuid();

        public void Commit()
        {
        }

        public Task CommitAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public void Rollback()
        {
        }

        public Task RollbackAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public void Dispose()
        {
        }

        public ValueTask DisposeAsync()
            => ValueTask.CompletedTask;
    }
}
