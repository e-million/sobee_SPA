using Sobee.Domain.Entities.Cart;
using Sobee.Domain.Entities.Promotions;
using Sobee.Domain.Repositories;
using sobee_API.Domain;
using sobee_API.DTOs;
using sobee_API.DTOs.Cart;
using sobee_API.Services.Interfaces;

namespace sobee_API.Services;

public sealed class CartService : ICartService
{
    private readonly ICartRepository _cartRepository;
    private readonly IProductRepository _productRepository;
    private readonly IPromoRepository _promoRepository;
    private readonly GuestSessionService _guestSessionService;

    public CartService(
        ICartRepository cartRepository,
        IProductRepository productRepository,
        IPromoRepository promoRepository,
        GuestSessionService guestSessionService)
    {
        _cartRepository = cartRepository;
        _productRepository = productRepository;
        _promoRepository = promoRepository;
        _guestSessionService = guestSessionService;
    }

    public async Task<ServiceResult<CartResponseDto>> GetCartAsync(
        string? userId,
        string? sessionId,
        bool canMergeGuestSession)
    {
        var cart = await GetOrCreateCartAsync(userId, sessionId, canMergeGuestSession);
        var response = await ProjectCartAsync(cart, userId, sessionId);
        return ServiceResult<CartResponseDto>.Ok(response);
    }

    public async Task<ServiceResult<CartResponseDto>> GetExistingCartAsync(string? userId, string? sessionId)
    {
        var cart = await FindCartAsync(userId, sessionId);
        if (cart == null)
        {
            return Validation<CartResponseDto>("ValidationError", "No cart found for this owner.", null);
        }

        var response = await ProjectCartAsync(cart, userId, sessionId);
        return ServiceResult<CartResponseDto>.Ok(response);
    }

    public async Task<ServiceResult<CartResponseDto>> AddItemAsync(
        string? userId,
        string? sessionId,
        bool canMergeGuestSession,
        AddCartItemRequest request)
    {
        var product = await _productRepository.FindByIdAsync(request.ProductId);

        if (product == null)
        {
            return NotFound<CartResponseDto>(
                $"Product {request.ProductId} not found.",
                new { productId = request.ProductId });
        }

        var cart = await GetOrCreateCartAsync(userId, sessionId, canMergeGuestSession);
        var existingItem = cart.TcartItems.FirstOrDefault(i => i.IntProductId == request.ProductId);

        if (existingItem == null)
        {
            var stockCheck = StockValidator.Validate(product.IntStockAmount, request.Quantity);
            if (!stockCheck.IsValid)
            {
                return Conflict<CartResponseDto>(
                    "InsufficientStock",
                    "Insufficient stock.",
                    new { productId = product.IntProductId, availableStock = product.IntStockAmount, requested = request.Quantity });
            }

            var newItem = new TcartItem
            {
                IntShoppingCartId = cart.IntShoppingCartId,
                IntProductId = request.ProductId,
                IntQuantity = request.Quantity,
                DtmDateAdded = DateTime.UtcNow
            };

            await _cartRepository.AddCartItemAsync(newItem);
        }
        else
        {
            var newQuantity = (existingItem.IntQuantity ?? 0) + request.Quantity;
            var stockCheck = StockValidator.Validate(product.IntStockAmount, newQuantity);
            if (!stockCheck.IsValid)
            {
                return Conflict<CartResponseDto>(
                    "InsufficientStock",
                    "Insufficient stock.",
                    new { productId = product.IntProductId, availableStock = product.IntStockAmount, requested = newQuantity });
            }

            existingItem.IntQuantity = newQuantity;
            await _cartRepository.UpdateCartItemAsync(existingItem);
        }

        cart.DtmDateLastUpdated = DateTime.UtcNow;
        await _cartRepository.SaveChangesAsync();

        var updated = await _cartRepository.LoadCartWithItemsAsync(cart.IntShoppingCartId);
        var response = await ProjectCartAsync(updated, userId, sessionId);
        return ServiceResult<CartResponseDto>.Ok(response);
    }

    public async Task<ServiceResult<CartResponseDto>> UpdateItemAsync(
        string? userId,
        string? sessionId,
        int cartItemId,
        UpdateCartItemRequest request)
    {
        var cart = await FindCartAsync(userId, sessionId);
        if (cart == null)
        {
            return NotFound<CartResponseDto>("Cart not found.", null);
        }

        var item = await _cartRepository.FindCartItemByIdAsync(cartItemId);
        if (item == null || item.IntShoppingCartId != cart.IntShoppingCartId)
        {
            return NotFound<CartResponseDto>(
                $"Cart item {cartItemId} not found.",
                new { cartItemId });
        }

        if (request.Quantity == 0)
        {
            await _cartRepository.RemoveCartItemAsync(item);
        }
        else
        {
            var product = item.IntProductId.HasValue
                ? await _productRepository.FindByIdAsync(item.IntProductId.Value)
                : null;

            if (product == null)
            {
                return NotFound<CartResponseDto>(
                    $"Product {item.IntProductId} not found.",
                    new { productId = item.IntProductId });
            }

            var stockCheck = StockValidator.Validate(product.IntStockAmount, request.Quantity);
            if (!stockCheck.IsValid)
            {
                return Conflict<CartResponseDto>(
                    "InsufficientStock",
                    "Insufficient stock.",
                    new { productId = product.IntProductId, availableStock = product.IntStockAmount, requested = request.Quantity });
            }

            item.IntQuantity = request.Quantity;
            await _cartRepository.UpdateCartItemAsync(item);
        }

        cart.DtmDateLastUpdated = DateTime.UtcNow;
        await _cartRepository.SaveChangesAsync();

        var updated = await _cartRepository.LoadCartWithItemsAsync(cart.IntShoppingCartId);
        var response = await ProjectCartAsync(updated, userId, sessionId);
        return ServiceResult<CartResponseDto>.Ok(response);
    }

    public async Task<ServiceResult<CartResponseDto>> RemoveItemAsync(
        string? userId,
        string? sessionId,
        int cartItemId)
    {
        var cart = await FindCartAsync(userId, sessionId);
        if (cart == null)
        {
            return NotFound<CartResponseDto>("Cart not found.", null);
        }

        var item = await _cartRepository.FindCartItemByIdAsync(cartItemId);
        if (item == null || item.IntShoppingCartId != cart.IntShoppingCartId)
        {
            return NotFound<CartResponseDto>(
                $"Cart item {cartItemId} not found.",
                new { cartItemId });
        }

        await _cartRepository.RemoveCartItemAsync(item);
        cart.DtmDateLastUpdated = DateTime.UtcNow;
        await _cartRepository.SaveChangesAsync();

        var updated = await _cartRepository.LoadCartWithItemsAsync(cart.IntShoppingCartId);
        var response = await ProjectCartAsync(updated, userId, sessionId);
        return ServiceResult<CartResponseDto>.Ok(response);
    }

    public async Task<ServiceResult<CartResponseDto>> ClearCartAsync(string? userId, string? sessionId)
    {
        var cart = await FindCartAsync(userId, sessionId);
        if (cart == null)
        {
            return NotFound<CartResponseDto>("Cart not found.", null);
        }

        await _cartRepository.ClearCartItemsAsync(cart.IntShoppingCartId);
        cart.DtmDateLastUpdated = DateTime.UtcNow;
        await _cartRepository.SaveChangesAsync();

        var updated = await _cartRepository.LoadCartWithItemsAsync(cart.IntShoppingCartId);
        var response = await ProjectCartAsync(updated, userId, sessionId);
        return ServiceResult<CartResponseDto>.Ok(response);
    }

    public async Task<ServiceResult<PromoAppliedResponseDto>> ApplyPromoAsync(
        string? userId,
        string? sessionId,
        string promoCode)
    {
        var cart = await FindCartAsync(userId, sessionId);
        if (cart == null)
        {
            return NotFound<PromoAppliedResponseDto>("Cart not found.", null);
        }

        var trimmedCode = promoCode.Trim();

        var promo = await _promoRepository.FindActiveByCodeAsync(trimmedCode, DateTime.UtcNow);

        if (promo == null)
        {
            return Validation<PromoAppliedResponseDto>("InvalidPromo", "Invalid or expired promo code.", null);
        }

        var alreadyApplied = await _promoRepository.UsageExistsAsync(cart.IntShoppingCartId, trimmedCode);

        if (alreadyApplied)
        {
            return Conflict<PromoAppliedResponseDto>(
                "Conflict",
                "Promo code already applied to this cart.",
                new { promoCode = trimmedCode });
        }

        await _promoRepository.AddUsageAsync(new TpromoCodeUsageHistory
        {
            IntShoppingCartId = cart.IntShoppingCartId,
            PromoCode = trimmedCode,
            UsedDateTime = DateTime.UtcNow
        });

        await _promoRepository.SaveChangesAsync();

        return ServiceResult<PromoAppliedResponseDto>.Ok(new PromoAppliedResponseDto
        {
            Message = "Promo code applied.",
            PromoCode = trimmedCode,
            DiscountPercentage = promo.DecDiscountPercentage
        });
    }

    public async Task<ServiceResult<MessageResponseDto>> RemovePromoAsync(string? userId, string? sessionId)
    {
        var cart = await FindCartAsync(userId, sessionId);
        if (cart == null)
        {
            return NotFound<MessageResponseDto>("Cart not found.", null);
        }

        var promos = await _promoRepository.GetUsagesForCartAsync(cart.IntShoppingCartId);

        if (promos.Count == 0)
        {
            return Validation<MessageResponseDto>(
                "ValidationError",
                "No promo code applied to cart.",
                null);
        }

        await _promoRepository.RemoveUsagesAsync(promos);
        await _promoRepository.SaveChangesAsync();

        return ServiceResult<MessageResponseDto>.Ok(new MessageResponseDto
        {
            Message = "Promo code removed."
        });
    }

    private async Task<TshoppingCart?> FindCartAsync(string? userId, string? sessionId)
    {
        if (!string.IsNullOrWhiteSpace(userId))
        {
            return await _cartRepository.FindByUserIdAsync(userId);
        }

        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            return await _cartRepository.FindBySessionIdAsync(sessionId);
        }

        return null;
    }

    private async Task<TshoppingCart> GetOrCreateCartAsync(string? userId, string? sessionId, bool canMergeGuestSession)
    {
        TshoppingCart? userCart = null;
        TshoppingCart? sessionCart = null;

        if (!string.IsNullOrWhiteSpace(userId))
        {
            userCart = await _cartRepository.FindByUserIdAsync(userId);
        }

        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            sessionCart = await _cartRepository.FindBySessionIdAsync(sessionId);
        }

        if (!string.IsNullOrWhiteSpace(userId) && sessionCart != null && canMergeGuestSession)
        {
            if (userCart == null)
            {
                sessionCart.UserId = userId;
                sessionCart.SessionId = null;
                sessionCart.DtmDateLastUpdated = DateTime.UtcNow;

                await _cartRepository.SaveChangesAsync();
                await RotateGuestSessionAsync(sessionId);
                return await _cartRepository.LoadCartWithItemsAsync(sessionCart.IntShoppingCartId);
            }

            foreach (var sessionItem in sessionCart.TcartItems.ToList())
            {
                if (sessionItem.IntProductId == null)
                {
                    continue;
                }

                var existing = userCart.TcartItems
                    .FirstOrDefault(i => i.IntProductId == sessionItem.IntProductId);

                if (existing == null)
                {
                    userCart.TcartItems.Add(new TcartItem
                    {
                        IntShoppingCartId = userCart.IntShoppingCartId,
                        IntProductId = sessionItem.IntProductId,
                        IntQuantity = sessionItem.IntQuantity ?? 0,
                        DtmDateAdded = DateTime.UtcNow
                    });
                }
                else
                {
                    existing.IntQuantity = (existing.IntQuantity ?? 0) + (sessionItem.IntQuantity ?? 0);
                }
            }

            userCart.DtmDateLastUpdated = DateTime.UtcNow;
            await _cartRepository.RemoveCartAsync(sessionCart);

            await _cartRepository.SaveChangesAsync();
            await RotateGuestSessionAsync(sessionId);
            return await _cartRepository.LoadCartWithItemsAsync(userCart.IntShoppingCartId);
        }

        if (userCart != null)
        {
            return await _cartRepository.LoadCartWithItemsAsync(userCart.IntShoppingCartId);
        }

        if (sessionCart != null)
        {
            return await _cartRepository.LoadCartWithItemsAsync(sessionCart.IntShoppingCartId);
        }

        var newCart = new TshoppingCart
        {
            UserId = userId,
            SessionId = sessionId,
            DtmDateCreated = DateTime.UtcNow,
            DtmDateLastUpdated = DateTime.UtcNow
        };

        await _cartRepository.CreateAsync(newCart);
        await _cartRepository.SaveChangesAsync();

        return await _cartRepository.LoadCartWithItemsAsync(newCart.IntShoppingCartId);
    }

    private async Task RotateGuestSessionAsync(string? sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return;
        }

        await _guestSessionService.InvalidateAsync(sessionId);
    }

    private async Task<CartResponseDto> ProjectCartAsync(TshoppingCart cart, string? userId, string? sessionId)
    {
        var items = cart.TcartItems.Select(i => new CartItemResponseDto
        {
            CartItemId = i.IntCartItemId,
            ProductId = i.IntProductId,
            Quantity = i.IntQuantity,
            Added = i.DtmDateAdded,
            Product = i.IntProduct == null ? null : new CartProductDto
            {
                Id = i.IntProduct.IntProductId,
                Name = i.IntProduct.StrName,
                Description = i.IntProduct.strDescription,
                Price = i.IntProduct.DecPrice,
                PrimaryImageUrl = GetPrimaryImageUrl(i.IntProduct)
            },
            LineTotal = (i.IntQuantity ?? 0) * (i.IntProduct?.DecPrice ?? 0m)
        }).ToList();

        var lineItems = cart.TcartItems
            .Select(i => new CartLineItem(i.IntQuantity ?? 0, i.IntProduct?.DecPrice ?? 0m));
        var subtotal = CartCalculator.CalculateSubtotal(lineItems);

        var promo = await GetActivePromoForCartAsync(cart.IntShoppingCartId);
        var discountAmount = PromoCalculator.CalculateDiscount(subtotal, promo.DiscountPercentage);
        var total = CartCalculator.CalculateTotal(subtotal, discountAmount);

        return new CartResponseDto
        {
            CartId = cart.IntShoppingCartId,
            Owner = userId != null ? "user" : "guest",
            UserId = userId,
            SessionId = sessionId,
            Created = cart.DtmDateCreated,
            Updated = cart.DtmDateLastUpdated,
            Items = items,
            Promo = promo.Code == null ? null : new CartPromoDto
            {
                Code = promo.Code,
                DiscountPercentage = promo.DiscountPercentage
            },
            Subtotal = subtotal,
            Discount = discountAmount,
            Total = total
        };
    }

    private async Task<(string? Code, decimal DiscountPercentage)> GetActivePromoForCartAsync(int cartId)
    {
        return await _promoRepository.GetActivePromoForCartAsync(cartId, DateTime.UtcNow);
    }

    private static string? GetPrimaryImageUrl(Sobee.Domain.Entities.Products.Tproduct product)
    {
        if (product.TproductImages == null || product.TproductImages.Count == 0)
        {
            return null;
        }

        return product.TproductImages
            .OrderBy(i => i.IntProductImageId)
            .Select(i => i.StrProductImageUrl)
            .FirstOrDefault();
    }

    private static ServiceResult<T> NotFound<T>(string message, object? data)
        => ServiceResult<T>.Fail("NotFound", message, data);

    private static ServiceResult<T> Validation<T>(string code, string message, object? data)
        => ServiceResult<T>.Fail(code, message, data);

    private static ServiceResult<T> Conflict<T>(string code, string message, object? data)
        => ServiceResult<T>.Fail(code, message, data);
}
