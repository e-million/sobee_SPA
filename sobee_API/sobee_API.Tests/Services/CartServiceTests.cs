using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Sobee.Domain.Data;
using Sobee.Domain.Entities.Cart;
using Sobee.Domain.Entities.Products;
using Sobee.Domain.Entities.Promotions;
using Sobee.Domain.Repositories;
using sobee_API.DTOs.Cart;
using sobee_API.Services;
using Xunit;

namespace sobee_API.Tests.Services;

public class CartServiceTests
{
    [Fact]
    public async Task GetCartAsync_NoCart_ReturnsEmptyCart()
    {
        using var context = new TestContext();

        var result = await context.Service.GetCartAsync(null, "session-1", false);

        result.Success.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Items.Should().BeEmpty();
        result.Value.Subtotal.Should().Be(0m);
        result.Value.Total.Should().Be(0m);
        context.CartRepository.Carts.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetCartAsync_ExistingCart_ReturnsWithItems()
    {
        using var context = new TestContext();
        var product = context.ProductRepository.AddProduct(CreateProduct(1, 4.50m, 10));
        var cart = context.CartRepository.AddCart(CreateCart(sessionId: "session-1"));
        context.CartRepository.AddCartItem(cart, product, 2);

        var result = await context.Service.GetCartAsync(null, "session-1", false);

        result.Success.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(1);
        result.Value.Items[0].Quantity.Should().Be(2);
        result.Value.Subtotal.Should().Be(9.00m);
        result.Value.Total.Should().Be(9.00m);
    }

    [Fact]
    public async Task GetCartAsync_AuthenticatedUser_MergesGuestCart()
    {
        using var context = new TestContext();
        var product1 = context.ProductRepository.AddProduct(CreateProduct(1, 5m, 10));
        var product2 = context.ProductRepository.AddProduct(CreateProduct(2, 6m, 10));

        var userCart = context.CartRepository.AddCart(CreateCart(userId: "user-1"));
        context.CartRepository.AddCartItem(userCart, product1, 1);

        var sessionCart = context.CartRepository.AddCart(CreateCart(sessionId: "session-1"));
        context.CartRepository.AddCartItem(sessionCart, product2, 2);

        var result = await context.Service.GetCartAsync("user-1", "session-1", true);

        result.Success.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(2);
        result.Value.Items.Select(i => i.ProductId).Should().Contain(new int?[] { 1, 2 });
        context.CartRepository.Carts.Should().ContainSingle(c => c.UserId == "user-1");
        context.CartRepository.Carts.Should().NotContain(c => c.SessionId == "session-1");
    }

    [Fact]
    public async Task AddItemAsync_NewItem_CreatesCartItem()
    {
        using var context = new TestContext();
        context.ProductRepository.AddProduct(CreateProduct(1, 5m, 10));

        var result = await context.Service.AddItemAsync(
            null,
            "session-1",
            false,
            new AddCartItemRequest { ProductId = 1, Quantity = 2 });

        result.Success.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(1);
        result.Value.Items[0].ProductId.Should().Be(1);
        result.Value.Items[0].Quantity.Should().Be(2);
    }

    [Fact]
    public async Task AddItemAsync_ExistingItem_IncrementsQuantity()
    {
        using var context = new TestContext();
        var product = context.ProductRepository.AddProduct(CreateProduct(1, 5m, 10));
        var cart = context.CartRepository.AddCart(CreateCart(sessionId: "session-1"));
        context.CartRepository.AddCartItem(cart, product, 1);

        var result = await context.Service.AddItemAsync(
            null,
            "session-1",
            false,
            new AddCartItemRequest { ProductId = 1, Quantity = 2 });

        result.Success.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(1);
        result.Value.Items[0].Quantity.Should().Be(3);
    }

    [Fact]
    public async Task AddItemAsync_InsufficientStock_ReturnsError()
    {
        using var context = new TestContext();
        context.ProductRepository.AddProduct(CreateProduct(1, 5m, 1));

        var result = await context.Service.AddItemAsync(
            null,
            "session-1",
            false,
            new AddCartItemRequest { ProductId = 1, Quantity = 2 });

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("InsufficientStock");
    }

    [Fact]
    public async Task AddItemAsync_InvalidProduct_ReturnsError()
    {
        using var context = new TestContext();

        var result = await context.Service.AddItemAsync(
            null,
            "session-1",
            false,
            new AddCartItemRequest { ProductId = 99, Quantity = 1 });

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("NotFound");
    }

    [Fact]
    public async Task UpdateItemAsync_ValidQuantity_UpdatesItem()
    {
        using var context = new TestContext();
        var product = context.ProductRepository.AddProduct(CreateProduct(1, 5m, 10));
        var cart = context.CartRepository.AddCart(CreateCart(sessionId: "session-1"));
        var item = context.CartRepository.AddCartItem(cart, product, 1);

        var result = await context.Service.UpdateItemAsync(
            null,
            "session-1",
            item.IntCartItemId,
            new UpdateCartItemRequest { Quantity = 3 });

        result.Success.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(1);
        result.Value.Items[0].Quantity.Should().Be(3);
    }

    [Fact]
    public async Task UpdateItemAsync_ZeroQuantity_RemovesItem()
    {
        using var context = new TestContext();
        var product = context.ProductRepository.AddProduct(CreateProduct(1, 5m, 10));
        var cart = context.CartRepository.AddCart(CreateCart(sessionId: "session-1"));
        var item = context.CartRepository.AddCartItem(cart, product, 1);

        var result = await context.Service.UpdateItemAsync(
            null,
            "session-1",
            item.IntCartItemId,
            new UpdateCartItemRequest { Quantity = 0 });

        result.Success.Should().BeTrue();
        result.Value!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateItemAsync_ExceedsStock_ReturnsError()
    {
        using var context = new TestContext();
        var product = context.ProductRepository.AddProduct(CreateProduct(1, 5m, 1));
        var cart = context.CartRepository.AddCart(CreateCart(sessionId: "session-1"));
        var item = context.CartRepository.AddCartItem(cart, product, 1);

        var result = await context.Service.UpdateItemAsync(
            null,
            "session-1",
            item.IntCartItemId,
            new UpdateCartItemRequest { Quantity = 2 });

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("InsufficientStock");
    }

    [Fact]
    public async Task UpdateItemAsync_ItemNotFound_ReturnsError()
    {
        using var context = new TestContext();
        context.CartRepository.AddCart(CreateCart(sessionId: "session-1"));

        var result = await context.Service.UpdateItemAsync(
            null,
            "session-1",
            999,
            new UpdateCartItemRequest { Quantity = 2 });

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("NotFound");
    }

    [Fact]
    public async Task RemoveItemAsync_ExistingItem_RemovesItem()
    {
        using var context = new TestContext();
        var product = context.ProductRepository.AddProduct(CreateProduct(1, 5m, 10));
        var cart = context.CartRepository.AddCart(CreateCart(sessionId: "session-1"));
        var item = context.CartRepository.AddCartItem(cart, product, 1);

        var result = await context.Service.RemoveItemAsync(null, "session-1", item.IntCartItemId);

        result.Success.Should().BeTrue();
        result.Value!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task RemoveItemAsync_ItemNotFound_ReturnsError()
    {
        using var context = new TestContext();
        context.CartRepository.AddCart(CreateCart(sessionId: "session-1"));

        var result = await context.Service.RemoveItemAsync(null, "session-1", 999);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("NotFound");
    }

    [Fact]
    public async Task ClearCartAsync_ClearsAllItems()
    {
        using var context = new TestContext();
        var product1 = context.ProductRepository.AddProduct(CreateProduct(1, 5m, 10));
        var product2 = context.ProductRepository.AddProduct(CreateProduct(2, 6m, 10));
        var cart = context.CartRepository.AddCart(CreateCart(sessionId: "session-1"));
        context.CartRepository.AddCartItem(cart, product1, 1);
        context.CartRepository.AddCartItem(cart, product2, 2);

        var result = await context.Service.ClearCartAsync(null, "session-1");

        result.Success.Should().BeTrue();
        result.Value!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task ApplyPromoAsync_ValidCode_AppliesDiscount()
    {
        using var context = new TestContext();
        var cart = context.CartRepository.AddCart(CreateCart(sessionId: "session-1"));
        context.PromoRepository.AddPromotion(new Tpromotion
        {
            IntPromotionId = 1,
            StrPromoCode = "SAVE10",
            StrDiscountPercentage = "10",
            DecDiscountPercentage = 10m,
            DtmExpirationDate = DateTime.UtcNow.AddDays(1)
        });

        var result = await context.Service.ApplyPromoAsync(null, "session-1", "SAVE10");

        result.Success.Should().BeTrue();
        result.Value!.PromoCode.Should().Be("SAVE10");
        result.Value.DiscountPercentage.Should().Be(10m);
        context.PromoRepository.Usages.Should().ContainSingle(u =>
            u.IntShoppingCartId == cart.IntShoppingCartId && u.PromoCode == "SAVE10");
    }

    [Fact]
    public async Task ApplyPromoAsync_ExpiredCode_ReturnsError()
    {
        using var context = new TestContext();
        context.CartRepository.AddCart(CreateCart(sessionId: "session-1"));
        context.PromoRepository.AddPromotion(new Tpromotion
        {
            IntPromotionId = 1,
            StrPromoCode = "EXPIRED",
            StrDiscountPercentage = "10",
            DecDiscountPercentage = 10m,
            DtmExpirationDate = DateTime.UtcNow.AddDays(-1)
        });

        var result = await context.Service.ApplyPromoAsync(null, "session-1", "EXPIRED");

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("InvalidPromo");
    }

    [Fact]
    public async Task ApplyPromoAsync_AlreadyUsed_ReturnsError()
    {
        using var context = new TestContext();
        var cart = context.CartRepository.AddCart(CreateCart(sessionId: "session-1"));
        context.PromoRepository.AddPromotion(new Tpromotion
        {
            IntPromotionId = 1,
            StrPromoCode = "SAVE10",
            StrDiscountPercentage = "10",
            DecDiscountPercentage = 10m,
            DtmExpirationDate = DateTime.UtcNow.AddDays(1)
        });
        context.PromoRepository.AddUsage(new TpromoCodeUsageHistory
        {
            IntShoppingCartId = cart.IntShoppingCartId,
            PromoCode = "SAVE10",
            UsedDateTime = DateTime.UtcNow.AddMinutes(-1)
        });

        var result = await context.Service.ApplyPromoAsync(null, "session-1", "SAVE10");

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("Conflict");
    }

    [Fact]
    public async Task RemovePromoAsync_RemovesAppliedPromo()
    {
        using var context = new TestContext();
        var cart = context.CartRepository.AddCart(CreateCart(sessionId: "session-1"));
        context.PromoRepository.AddUsage(new TpromoCodeUsageHistory
        {
            IntShoppingCartId = cart.IntShoppingCartId,
            PromoCode = "SAVE10",
            UsedDateTime = DateTime.UtcNow.AddMinutes(-1)
        });

        var result = await context.Service.RemovePromoAsync(null, "session-1");

        result.Success.Should().BeTrue();
        context.PromoRepository.Usages.Should().BeEmpty();
    }

    private static Tproduct CreateProduct(int id, decimal price, int stock)
        => new()
        {
            IntProductId = id,
            StrName = $"Product-{id}",
            strDescription = $"Product-{id}",
            DecPrice = price,
            IntStockAmount = stock
        };

    private static TshoppingCart CreateCart(string? userId = null, string? sessionId = null)
        => new()
        {
            UserId = userId,
            SessionId = sessionId,
            DtmDateCreated = DateTime.UtcNow,
            DtmDateLastUpdated = DateTime.UtcNow
        };

    private sealed class TestContext : IDisposable
    {
        private readonly SobeecoredbContext _dbContext;

        public CartService Service { get; }
        public FakeCartRepository CartRepository { get; }
        public FakeProductRepository ProductRepository { get; }
        public FakePromoRepository PromoRepository { get; }

        public TestContext()
        {
            _dbContext = CreateDbContext();
            ProductRepository = new FakeProductRepository();
            PromoRepository = new FakePromoRepository();
            CartRepository = new FakeCartRepository(ProductRepository.FindById);

            var guestSessionRepository = new GuestSessionRepository(_dbContext);
            var guestSessionService = new GuestSessionService(
                guestSessionRepository,
                NullLogger<GuestSessionService>.Instance);

            Service = new CartService(
                CartRepository,
                ProductRepository,
                PromoRepository,
                guestSessionService);
        }

        public void Dispose()
        {
            _dbContext.Dispose();
        }

        private static SobeecoredbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<SobeecoredbContext>()
                .UseSqlite("DataSource=:memory:")
                .Options;

            var context = new SobeecoredbContext(options);
            context.Database.OpenConnection();
            context.Database.EnsureCreated();
            return context;
        }
    }

    private sealed class FakeProductRepository : IProductRepository
    {
        private readonly Dictionary<int, Tproduct> _products = new();

        public Tproduct AddProduct(Tproduct product)
        {
            _products[product.IntProductId] = product;
            return product;
        }

        public Task<Tproduct?> FindByIdAsync(int productId)
            => Task.FromResult(_products.TryGetValue(productId, out var product) ? product : null);

        public Task<IReadOnlyList<Tproduct>> GetByIdsAsync(IEnumerable<int> productIds, bool track = true)
        {
            IReadOnlyList<Tproduct> products = productIds
                .Distinct()
                .Select(id => _products.TryGetValue(id, out var product) ? product : null)
                .Where(product => product != null)
                .Cast<Tproduct>()
                .ToList();

            return Task.FromResult(products);
        }

        public Task<(IReadOnlyList<Tproduct> Items, int TotalCount)> GetProductsAsync(
            string? query,
            string? category,
            int page,
            int pageSize,
            string? sort,
            bool track = false)
        {
            IReadOnlyList<Tproduct> products = _products.Values.ToList();
            return Task.FromResult((products, products.Count));
        }

        public Task<Tproduct?> FindByIdWithImagesAsync(int productId, bool track = false)
            => FindByIdAsync(productId);

        public Task<bool> ExistsAsync(int productId)
            => Task.FromResult(_products.ContainsKey(productId));

        public Task<TproductImage?> FindImageAsync(int productId, int imageId)
            => Task.FromResult<TproductImage?>(null);

        public Task AddAsync(Tproduct product)
        {
            _products[product.IntProductId] = product;
            return Task.CompletedTask;
        }

        public Task AddImageAsync(TproductImage image)
            => Task.CompletedTask;

        public Task RemoveAsync(Tproduct product)
        {
            _products.Remove(product.IntProductId);
            return Task.CompletedTask;
        }

        public Task RemoveImageAsync(TproductImage image)
            => Task.CompletedTask;

        public Task SaveChangesAsync()
            => Task.CompletedTask;

        public Tproduct? FindById(int productId)
            => _products.TryGetValue(productId, out var product) ? product : null;
    }

    private sealed class FakePromoRepository : IPromoRepository
    {
        private readonly List<Tpromotion> _promotions = new();
        private readonly List<TpromoCodeUsageHistory> _usages = new();
        private int _nextUsageId = 1;

        public IReadOnlyList<TpromoCodeUsageHistory> Usages => _usages;

        public void AddPromotion(Tpromotion promotion)
        {
            _promotions.Add(promotion);
        }

        public void AddUsage(TpromoCodeUsageHistory usage)
        {
            if (usage.IntUsageHistoryId == 0)
            {
                usage.IntUsageHistoryId = _nextUsageId++;
            }

            _usages.Add(usage);
        }

        public Task<Tpromotion?> FindActiveByCodeAsync(string promoCode, DateTime utcNow)
        {
            var promo = _promotions.FirstOrDefault(p =>
                p.StrPromoCode == promoCode &&
                p.DtmExpirationDate > utcNow);
            return Task.FromResult(promo);
        }

        public Task<bool> UsageExistsAsync(int cartId, string promoCode)
            => Task.FromResult(_usages.Any(u => u.IntShoppingCartId == cartId && u.PromoCode == promoCode));

        public Task<IReadOnlyList<TpromoCodeUsageHistory>> GetUsagesForCartAsync(int cartId)
        {
            IReadOnlyList<TpromoCodeUsageHistory> results = _usages
                .Where(u => u.IntShoppingCartId == cartId)
                .ToList();
            return Task.FromResult(results);
        }

        public Task AddUsageAsync(TpromoCodeUsageHistory usage)
        {
            AddUsage(usage);
            return Task.CompletedTask;
        }

        public Task RemoveUsagesAsync(IEnumerable<TpromoCodeUsageHistory> usages)
        {
            foreach (var usage in usages.ToList())
            {
                _usages.Remove(usage);
            }
            return Task.CompletedTask;
        }

        public Task<(string? Code, decimal DiscountPercentage)> GetActivePromoForCartAsync(int cartId, DateTime utcNow)
        {
            var promo = _usages
                .Where(u => u.IntShoppingCartId == cartId)
                .OrderByDescending(u => u.UsedDateTime ?? DateTime.MinValue)
                .Select(u => new
                {
                    Usage = u,
                    Promotion = _promotions.FirstOrDefault(p =>
                        p.StrPromoCode == u.PromoCode &&
                        p.DtmExpirationDate > utcNow)
                })
                .FirstOrDefault(x => x.Promotion != null);

            if (promo?.Promotion == null)
            {
                return Task.FromResult<(string?, decimal)>((null, 0m));
            }

            return Task.FromResult<(string?, decimal)>(
                (promo.Promotion.StrPromoCode, promo.Promotion.DecDiscountPercentage));
        }

        public Task SaveChangesAsync() => Task.CompletedTask;
    }

    private sealed class FakeCartRepository : ICartRepository
    {
        private readonly Func<int, Tproduct?> _productLookup;
        private readonly List<TshoppingCart> _carts = new();
        private readonly List<TcartItem> _items = new();
        private int _nextCartId = 1;
        private int _nextItemId = 1;

        public FakeCartRepository(Func<int, Tproduct?> productLookup)
        {
            _productLookup = productLookup;
        }

        public IReadOnlyList<TshoppingCart> Carts => _carts;

        public TshoppingCart AddCart(TshoppingCart cart)
        {
            if (cart.IntShoppingCartId == 0)
            {
                cart.IntShoppingCartId = _nextCartId++;
            }

            _carts.Add(cart);
            return cart;
        }

        public TcartItem AddCartItem(TshoppingCart cart, Tproduct product, int quantity, int? itemId = null)
        {
            var item = new TcartItem
            {
                IntCartItemId = itemId ?? _nextItemId++,
                IntShoppingCartId = cart.IntShoppingCartId,
                IntShoppingCart = cart,
                IntProductId = product.IntProductId,
                IntProduct = product,
                IntQuantity = quantity,
                DtmDateAdded = DateTime.UtcNow
            };

            cart.TcartItems.Add(item);
            product.TcartItems.Add(item);
            _items.Add(item);
            return item;
        }

        public Task<TshoppingCart?> FindByUserIdAsync(string userId)
        {
            var cart = _carts.FirstOrDefault(c => c.UserId == userId);
            AttachItems(cart);
            return Task.FromResult(cart);
        }

        public Task<TshoppingCart?> FindBySessionIdAsync(string sessionId)
        {
            var cart = _carts.FirstOrDefault(c => c.SessionId == sessionId && c.UserId == null);
            AttachItems(cart);
            return Task.FromResult(cart);
        }

        public Task<TshoppingCart> CreateAsync(TshoppingCart cart)
        {
            AddCart(cart);
            return Task.FromResult(cart);
        }

        public Task UpdateAsync(TshoppingCart cart)
        {
            return Task.CompletedTask;
        }

        public Task<TcartItem?> FindCartItemAsync(int cartId, int productId)
            => Task.FromResult(_items.FirstOrDefault(i => i.IntShoppingCartId == cartId && i.IntProductId == productId));

        public Task<TcartItem?> FindCartItemByIdAsync(int cartItemId)
            => Task.FromResult(_items.FirstOrDefault(i => i.IntCartItemId == cartItemId));

        public Task AddCartItemAsync(TcartItem item)
        {
            if (item.IntCartItemId == 0)
            {
                item.IntCartItemId = _nextItemId++;
            }

            if (item.IntShoppingCartId.HasValue)
            {
                var cart = _carts.FirstOrDefault(c => c.IntShoppingCartId == item.IntShoppingCartId);
                if (cart != null && !cart.TcartItems.Contains(item))
                {
                    cart.TcartItems.Add(item);
                    item.IntShoppingCart = cart;
                }
            }

            if (item.IntProductId.HasValue && item.IntProduct == null)
            {
                item.IntProduct = _productLookup(item.IntProductId.Value);
            }

            _items.Add(item);
            return Task.CompletedTask;
        }

        public Task UpdateCartItemAsync(TcartItem item)
            => Task.CompletedTask;

        public Task RemoveCartItemAsync(TcartItem item)
        {
            _items.Remove(item);
            item.IntShoppingCart?.TcartItems.Remove(item);
            return Task.CompletedTask;
        }

        public Task RemoveCartAsync(TshoppingCart cart)
        {
            var items = _items.Where(i => i.IntShoppingCartId == cart.IntShoppingCartId).ToList();
            foreach (var item in items)
            {
                _items.Remove(item);
            }

            _carts.Remove(cart);
            return Task.CompletedTask;
        }

        public Task ClearCartItemsAsync(int cartId)
        {
            var cart = _carts.FirstOrDefault(c => c.IntShoppingCartId == cartId);
            if (cart != null)
            {
                cart.TcartItems.Clear();
            }

            _items.RemoveAll(i => i.IntShoppingCartId == cartId);
            return Task.CompletedTask;
        }

        public Task<TshoppingCart> LoadCartWithItemsAsync(int cartId)
        {
            var cart = _carts.First(c => c.IntShoppingCartId == cartId);
            AttachItems(cart);
            return Task.FromResult(cart);
        }

        public Task SaveChangesAsync()
        {
            SyncItemsFromCarts();
            return Task.CompletedTask;
        }

        private void SyncItemsFromCarts()
        {
            foreach (var cart in _carts)
            {
                foreach (var item in cart.TcartItems)
                {
                    var exists = item.IntCartItemId != 0
                        ? _items.Any(i => i.IntCartItemId == item.IntCartItemId)
                        : _items.Contains(item);

                    if (exists)
                    {
                        continue;
                    }

                    if (item.IntCartItemId == 0)
                    {
                        item.IntCartItemId = _nextItemId++;
                    }

                    item.IntShoppingCartId ??= cart.IntShoppingCartId;
                    item.IntShoppingCart ??= cart;

                    if (item.IntProductId.HasValue && item.IntProduct == null)
                    {
                        item.IntProduct = _productLookup(item.IntProductId.Value);
                    }

                    _items.Add(item);
                }
            }
        }

        private void AttachItems(TshoppingCart? cart)
        {
            if (cart == null)
            {
                return;
            }

            cart.TcartItems = _items
                .Where(i => i.IntShoppingCartId == cart.IntShoppingCartId)
                .ToList();

            foreach (var item in cart.TcartItems)
            {
                item.IntShoppingCart ??= cart;
                if (item.IntProductId.HasValue && item.IntProduct == null)
                {
                    item.IntProduct = _productLookup(item.IntProductId.Value);
                }
            }
        }
    }
}
