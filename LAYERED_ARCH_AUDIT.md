# Layered Architecture Audit Report

<span style="background-color:#b91c1c;color:#ffffff;padding:2px 6px;border-radius:4px;"><strong>Compliance: LOW</strong></span>

**Sobee E-Commerce API (ASP.NET Core 8.0)**

## 1) Executive Summary

**Overall Compliance:** <span style="color:#b91c1c"><strong>LOW</strong></span>
The codebase follows a monolithic "Active Record" / "Transaction Script" pattern rather than the expected Controller-Service-Repository layered architecture. Controllers directly inject and use DbContext for all data access and contain extensive business logic inline.

### Biggest Risks (Top 5)

| Rank | Risk | Impact |
| --- | --- | --- |
| 1 | All 14 domain controllers inject DbContext directly | Untestable, tightly coupled, no abstraction layer |
| 2 | Business logic (pricing, discounts, stock, checkout workflow) embedded in controllers | Duplicated logic, error-prone changes, no reuse |
| 3 | No service layer for domain operations | Cannot unit test business rules without spinning up database |
| 4 | No repository abstraction | EF queries scattered across controllers, difficult to optimize |
| 5 | Transaction management in controllers | Inconsistent transactional boundaries, risk of partial commits |

### Estimated Effort by Subsystem

| Subsystem | Violation Count | Effort | Priority |
| --- | --- | --- | --- |
| Cart | 9 EF queries in controller | L | Critical |
| Orders/Checkout | 12+ EF queries, transaction logic in controller | L | Critical |
| Products | 8 EF queries in controller | M | High |
| Reviews | 6 EF queries in controller | M | Medium |
| Favorites | 4 EF queries in controller | S | Low |
| Admin Analytics | 15+ complex EF queries | L | Medium |
| Admin Promos | 6 EF queries in controller | M | Low |
| Admin Users | 5 EF queries in controller | M | Low |
| Auth/Users | UserManager only (acceptable) | S | Low |
| Payments | 1 EF query | S | Low |

Legend: S = Small (<2 days), M = Medium (2-5 days), L = Large (5+ days)

## 2) Architecture Map

### Projects/Assemblies

| Project | Role | Dependencies |
| --- | --- | --- |
| sobee_API | Presentation Layer (Controllers, DTOs, Validators, Middleware) | Sobee.Domain |
| Sobee.Domain | Data Access + Domain Entities (DbContexts, Entities) | EF Core, Identity |
| sobee_API.Tests | Integration Tests | sobee_API |

### Current Architecture Diagram

```text
Presentation Layer (sobee_API)
  - Controllers (14), DTOs (23+), Validators (13+)
  - Direct DbContext injection (no service/repository)
  - Utility services: GuestSessionService, RequestIdentityResolver, IdentitySeedService, RoleSeedService
        |
        v
Data Access Layer (Sobee.Domain)
  - DbContexts: SobeecoredbContext, ApplicationDbContext
  - Entities: Cart, Orders, Products, Payments, Reviews, Identity
        |
        v
SQL Server
```

### Layer Violations Summary

| Violation Type | Count | Examples |
| --- | --- | --- |
| Controller -> DbContext direct injection | 14 | All domain controllers |
| Business logic in controllers | ~50 methods | Checkout, AddItem, ApplyPromo |
| EF LINQ queries in controllers | 60+ | Throughout all controllers |
| Transaction management in controllers | 3 | OrdersController (Checkout, Pay, etc.) |
| Missing service layer | 100% | No domain services exist |
| Missing repository layer | 100% | No repositories exist |

### Circular Dependencies
None detected. The dependency flow is strictly: sobee_API -> Sobee.Domain.

## 3) Findings (with Evidence)

### LAYER-001: Controllers Inject DbContext Directly (Blocker)
**Severity:** <span style="color:#b91c1c"><strong>Blocker</strong></span>

**Rule Violated:** Controllers must NOT use DbContext directly

**Files Affected:**

| Controller | File Path |
| --- | --- |
| CartController | CartController.cs:18 |
| OrdersController | OrdersController.cs:24 |
| ProductsController | ProductsController.cs:15 |
| ReviewsController | ReviewsController.cs:15 |
| FavoritesController | FavoritesController.cs:17 |
| PaymentMethodsController | PaymentMethodsController.cs:13 |
| AdminAnalyticsController | AdminAnalyticsController.cs:15 |
| AdminPromosController | AdminPromosController.cs:17 |
| AdminUsersController | AdminUsersController.cs:17 |

**Code Excerpt (CartController):**


```csharp
public class CartController : ApiControllerBase
{
    private readonly SobeecoredbContext _db;  // <-- VIOLATION

    public CartController(SobeecoredbContext db, ...)
    {
        _db = db;  // Direct injection
    }
```
**Why It's a Problem:**

- Cannot unit test controller logic without database
- Tight coupling makes refactoring dangerous
- No abstraction for query optimization
- Violates Single Responsibility Principle
### LAYER-002: Business Logic in Controllers (Blocker)
**Severity:** <span style="color:#b91c1c"><strong>Blocker</strong></span>

**Rule Violated:** Business workflows must be in service layer

**Files Affected: CartController, OrdersController**

**Example 1: Cart Merge Logic (CartController.cs:364-430)**


```csharp
private async Task<TshoppingCart> GetOrCreateCartAsync(...)
{
    // 60+ lines of cart creation, merging, session rotation logic
    if (!string.IsNullOrWhiteSpace(userId) && sessionCart != null && canMergeGuestSession)
    {
        if (userCart == null)
        {
            sessionCart.UserId = userId;
            sessionCart.SessionId = null;
            // ... business logic for claiming cart
        }
        // Merge items: add quantities into userCart
        foreach (var sessionItem in sessionCart.TcartItems.ToList())
        {
            // ... merge logic
        }
    }
}
```
**Example 2: Checkout Workflow (OrdersController.cs:110-279)**


```csharp
public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request)
{
    // 170 lines of:
    // - Cart validation
    // - Subtotal calculation
    // - Promo discount calculation
    // - Stock validation and decrement
    // - Order creation
    // - Order items creation
    // - Cart clearing
    // - Transaction management
}
```
**Example 3: Promo Discount Calculation (CartController.cs:492-505)**


```csharp
var discountAmount = 0m;
if (promo.DiscountPercentage > 0 && subtotal > 0)
{
    discountAmount = subtotal * (promo.DiscountPercentage / 100m);
}
var total = subtotal - discountAmount;
if (total < 0) total = 0;
```
**Why It's a Problem:**

- Business rules are duplicated (discount calculation appears in Cart AND Orders)
- Cannot reuse checkout logic from other entry points
- Unit testing requires HTTP context mocking
- Changes to pricing rules require modifying controllers
### LAYER-003: EF Queries Scattered in Controllers (Major)
**Severity:** <span style="color:#f97316"><strong>Major</strong></span>

**Rule Violated:** Data access must be isolated in repositories

**Files Affected: All domain controllers**

### Examples:

CartController - 9+ distinct EF queries:


// Line 61-62
var product = await _db.Tproducts
    .FirstOrDefaultAsync(p => p.IntProductId == request.ProductId);

// Line 77-79
var existingItem = await _db.TcartItems.FirstOrDefaultAsync(i =>
    i.IntShoppingCartId == cart.IntShoppingCartId &&
    i.IntProductId == request.ProductId);

// Line 132-134
var promo = await _db.Tpromotions.FirstOrDefaultAsync(p =>
    p.StrPromoCode == promoCode &&
    p.DtmExpirationDate > DateTime.UtcNow);

// Line 529-541 (complex join)
var promo = await _db.TpromoCodeUsageHistories
    .Join(_db.Tpromotions,
        usage => usage.PromoCode,
        promo => promo.StrPromoCode,
        (usage, promo) => new { usage, promo })
    .Where(x => x.usage.IntShoppingCartId == cartId && ...)
AdminAnalyticsController - 15+ complex analytical queries spanning 600+ lines

**Why It's a Problem:**

- Query optimization is scattered and hard to audit
- Duplicate query patterns across controllers
- Cannot easily add caching layer
- Difficult to profile database performance
### LAYER-004: Transaction Management in Controllers (Major)
**Severity:** <span style="color:#f97316"><strong>Major</strong></span>

**Rule Violated:** Transactional boundaries should be managed by service layer

**Files Affected: OrdersController.cs**

**Checkout Transaction (Lines 182-278):**


```csharp
using var tx = await _db.Database.BeginTransactionAsync();
try
{
    // 1) Validate stock and decrement it
    foreach (var cartItem in cart.TcartItems)
    {
        if (product.IntStockAmount < qty)
            return ConflictError(...);  // <-- Returns from inside transaction
        product.IntStockAmount -= qty;
    }
    // 2) Create order header
    // 3) Create order items
    // 4) Clear cart items
    // 5) Clear applied promos
    await _db.SaveChangesAsync();
    await tx.CommitAsync();
}
catch
{
    await tx.RollbackAsync();
    return ServerError("Checkout failed.");
}
```
**PayOrder Transaction (Lines 377-406):**


```csharp
using var tx = await _db.Database.BeginTransactionAsync();
try
{
    var payment = new Tpayment { ... };
    _db.Tpayments.Add(payment);
    order.IntPaymentMethodId = paymentMethod.IntPaymentMethodId;
    order.StrOrderStatus = targetStatus;
    await _db.SaveChangesAsync();
    await tx.CommitAsync();
}
catch
{
    await tx.RollbackAsync();
    return ServerError("Payment failed.");
}
```
**Why It's a Problem:**

- Transaction logic mixed with HTTP response logic
- Early returns from transaction blocks can lead to leaks if not careful
- Cannot reuse transactional workflow from other contexts
- Difficult to test transaction rollback scenarios
### LAYER-005: Stock Validation Logic in Controller (Major)
**Severity:** <span style="color:#f97316"><strong>Major</strong></span>

**Rule Violated:** Inventory rules are business logic, not presentation logic

**Files Affected: CartController, OrdersController**

CartController (Lines 84-103):


// Stock check for new item
if (request.Quantity > product.IntStockAmount)
    return StockConflict(product.IntProductId, product.IntStockAmount, request.Quantity);

// Stock check for increment
if (newQuantity > product.IntStockAmount)
    return StockConflict(product.IntProductId, product.IntStockAmount, newQuantity);
OrdersController (Lines 186-207):


foreach (var cartItem in cart.TcartItems)
{
    if (product.IntStockAmount < qty)
    {
        return ConflictError("Insufficient stock.", ...);
    }
    product.IntStockAmount -= qty;  // Inventory modification
}
**Why It's a Problem:**

- Stock rules duplicated across Cart and Orders
- Cannot enforce stock policies uniformly
- Race conditions possible without proper service-level locking
### LAYER-006: Order Status Transition Logic in Controller (Major)
**Severity:** <span style="color:#f97316"><strong>Major</strong></span>

**Rule Violated:** Status workflow rules belong in service/domain layer

File: OrdersController.cs:305-320, 418-468


var currentStatus = OrderStatuses.Normalize(order.StrOrderStatus);
if (!OrderStatuses.IsCancellable(currentStatus))
{
    return ConflictError("Order cannot be cancelled...");
}

if (!OrderStatuses.CanTransition(currentStatus, targetStatus))
{
    return ConflictError("Invalid status transition.", ...);
}

// Status change logic
order.StrOrderStatus = newStatus;
if (string.Equals(newStatus, OrderStatuses.Shipped, ...))
{
    order.DtmShippedDate = DateTime.UtcNow;
}
**Why It's a Problem:**

- State machine logic in controller
- Workflow rules tied to HTTP layer
- Cannot trigger status changes from background jobs easily
### LAYER-007: DTO Projection in Controllers (Minor)
**Severity:** <span style="color:#eab308"><strong>Minor</strong></span>

**Rule Violated:** DTO mapping should be centralized

Files: All controllers

Example (CartController Lines 473-527):


private async Task<CartResponseDto> ProjectCartAsync(TshoppingCart cart, ...)
{
    var items = cart.TcartItems.Select(i => new CartItemResponseDto
    {
        CartItemId = i.IntCartItemId,
        ProductId = i.IntProductId,
        // ... 15+ property mappings
    }).ToList();

    return new CartResponseDto
    {
        CartId = cart.IntShoppingCartId,
        // ... more mappings
    };
}
**Why It's a Problem:**

- Mapping logic scattered across controllers
- No centralized mapping configuration (like AutoMapper profiles)
- Hard to maintain consistency across endpoints
### LAYER-008: Services Access HTTP Types Correctly (Positive Finding)
**Severity:** <span style="color:#0f172a"><strong>N/A - Compliant</strong></span>

Note: GuestSessionService and RequestIdentityResolver do accept HttpRequest/HttpResponse parameters, but these are infrastructure services, not domain services. This is an acceptable pattern for cross-cutting concerns like session management.

## 4) Inventory of Endpoints

### Cart Endpoints

| Route | Controller Action | Service Method | Repository Method |
| --- | --- | --- | --- |
| GET /api/cart | CartController.GetCart() | DIRECT DB ACCESS | N/A |
| POST /api/cart/items | CartController.AddItem() | DIRECT DB ACCESS | N/A |
| PUT /api/cart/items/{id} | CartController.UpdateItem() | DIRECT DB ACCESS | N/A |
| DELETE /api/cart/items/{id} | CartController.RemoveItem() | DIRECT DB ACCESS | N/A |
| DELETE /api/cart | CartController.ClearCart() | DIRECT DB ACCESS | N/A |
| POST /api/cart/promo/apply | CartController.ApplyPromo() | DIRECT DB ACCESS | N/A |
| DELETE /api/cart/promo | CartController.RemovePromo() | DIRECT DB ACCESS | N/A |

### Order Endpoints

| Route | Controller Action | Service Method | Repository Method |
| --- | --- | --- | --- |
| GET /api/orders/{id} | OrdersController.GetOrder() | DIRECT DB ACCESS | N/A |
| GET /api/orders/my | OrdersController.GetMyOrders() | DIRECT DB ACCESS | N/A |
| POST /api/orders/checkout | OrdersController.Checkout() | DIRECT DB ACCESS | N/A |
| POST /api/orders/{id}/cancel | OrdersController.CancelOrder() | DIRECT DB ACCESS | N/A |
| POST /api/orders/{id}/pay | OrdersController.PayOrder() | DIRECT DB ACCESS | N/A |
| PATCH /api/orders/{id}/status | OrdersController.UpdateOrderStatus() | DIRECT DB ACCESS | N/A |

### Product Endpoints

| Route | Controller Action | Service Method | Repository Method |
| --- | --- | --- | --- |
| GET /api/products | ProductsController.GetProducts() | DIRECT DB ACCESS | N/A |
| GET /api/products/{id} | ProductsController.GetProduct() | DIRECT DB ACCESS | N/A |
| POST /api/products | ProductsController.CreateProduct() | DIRECT DB ACCESS | N/A |
| PUT /api/products/{id} | ProductsController.UpdateProduct() | DIRECT DB ACCESS | N/A |
| DELETE /api/products/{id} | ProductsController.DeleteProduct() | DIRECT DB ACCESS | N/A |
| POST /api/products/{id}/images | ProductsController.AddProductImage() | DIRECT DB ACCESS | N/A |
| DELETE /api/products/{id}/images/{imgId} | ProductsController.DeleteProductImage() | DIRECT DB ACCESS | N/A |

Review Endpoints
| Route | Controller Action | Service Method | Repository Method |
| --- | --- | --- | --- |
| GET /api/reviews/product/{id} | ReviewsController.GetByProduct() | DIRECT DB ACCESS | N/A |
| POST /api/reviews/product/{id} | ReviewsController.Create() | DIRECT DB ACCESS | N/A |
| POST /api/reviews/{id}/reply | ReviewsController.Reply() | DIRECT DB ACCESS | N/A |
| DELETE /api/reviews/{id} | ReviewsController.DeleteReview() | DIRECT DB ACCESS | N/A |
| DELETE /api/reviews/replies/{id} | ReviewsController.DeleteReply() | DIRECT DB ACCESS | N/A |

Favorites Endpoints
| Route | Controller Action | Service Method | Repository Method |
| --- | --- | --- | --- |
| GET /api/favorites | FavoritesController.GetMyFavorites() | DIRECT DB ACCESS | N/A |
| POST /api/favorites/{productId} | FavoritesController.AddFavorite() | DIRECT DB ACCESS | N/A |
| DELETE /api/favorites/{productId} | FavoritesController.RemoveFavorite() | DIRECT DB ACCESS | N/A |

Admin Analytics (all DIRECT DB ACCESS)
| Route | Controller Action |
| --- | --- |
| GET /api/admin/analytics/revenue | GetRevenueByPeriod() |
| GET /api/admin/analytics/orders/status | GetOrderStatusBreakdown() |
| GET /api/admin/analytics/reviews/* | 4 endpoints |
| GET /api/admin/analytics/products/* | 2 endpoints |
| GET /api/admin/analytics/inventory/summary | GetInventorySummary() |
| GET /api/admin/analytics/orders/fulfillment | GetFulfillmentMetrics() |
| GET /api/admin/analytics/customers/* | 4 endpoints |
| GET /api/admin/analytics/wishlist/top | GetMostWishlisted() |

Admin Promos (all DIRECT DB ACCESS)
| Route | Controller Action |
| --- | --- |
| GET /api/admin/promos | GetPromos() |
| POST /api/admin/promos | CreatePromo() |
| PUT /api/admin/promos/{id} | UpdatePromo() |
| DELETE /api/admin/promos/{id} | DeletePromo() |

Admin Users (all DIRECT DB ACCESS via ApplicationDbContext)
| Route | Controller Action |
| --- | --- |
| GET /api/admin/users | GetUsers() |
| PUT /api/admin/users/{id}/admin | UpdateAdminRole() |

Auth/Users (UserManager - acceptable)
| Route | Controller Action | Service |
| --- | --- | --- |
| POST /api/auth/register | AuthController.Register() | UserManager ok |
| GET /api/users/profile | UsersController.GetProfile() | UserManager ok |
| PUT /api/users/profile | UsersController.UpdateProfile() | UserManager ok |
| PUT /api/users/password | UsersController.UpdatePassword() | UserManager ok |

Other
| Route | Controller Action | Service |
| --- | --- | --- |
| GET /api/me | MeController.Get() | ClaimsPrincipal ok |
| GET /api/paymentmethods | PaymentMethodsController.GetPaymentMethods() | DIRECT DB ACCESS |

## 5) Recommendations (Target Patterns)

5.1 Standard Interface Patterns
Services (Application Layer):


// File: sobee_API/Services/ICartService.cs
public interface ICartService
{
    Task<CartDto> GetCartAsync(string... userId, string... sessionId);
    Task<CartDto> AddItemAsync(string... userId, string... sessionId, int productId, int quantity);
    Task<CartDto> UpdateItemAsync(string... userId, string... sessionId, int cartItemId, int quantity);
    Task<CartDto> RemoveItemAsync(string... userId, string... sessionId, int cartItemId);
    Task<CartDto> ClearCartAsync(string... userId, string... sessionId);
    Task<PromoResult> ApplyPromoAsync(string... userId, string... sessionId, string promoCode);
    Task<CartDto> RemovePromoAsync(string... userId, string... sessionId);
}

// File: sobee_API/Services/IOrderService.cs
public interface IOrderService
{
    Task<OrderDto> GetOrderAsync(string... userId, string... sessionId, int orderId);
    Task<PagedResult<OrderDto>> GetUserOrdersAsync(string userId, int page, int pageSize);
    Task<OrderDto> CheckoutAsync(string... userId, string... sessionId, CheckoutRequest request);
    Task<OrderDto> CancelOrderAsync(string... userId, string... sessionId, int orderId);
    Task<OrderDto> PayOrderAsync(string... userId, string... sessionId, int orderId, PayOrderRequest request);
    Task<OrderDto> UpdateStatusAsync(int orderId, string newStatus); // Admin
}

// File: sobee_API/Services/IProductService.cs
public interface IProductService
{
    Task<PagedResult<ProductListDto>> GetProductsAsync(ProductQueryParams query, bool isAdmin);
    Task<ProductDetailDto> GetProductAsync(int productId, bool isAdmin);
    Task<ProductDetailDto> CreateProductAsync(CreateProductRequest request);
    Task<ProductDetailDto> UpdateProductAsync(int productId, UpdateProductRequest request);
    Task DeleteProductAsync(int productId);
}
Repositories (Data Layer):


// File: Sobee.Domain/Repositories/ICartRepository.cs
public interface ICartRepository
{
    Task<TshoppingCart...> FindByUserIdAsync(string userId);
    Task<TshoppingCart...> FindBySessionIdAsync(string sessionId);
    Task<TshoppingCart> CreateAsync(TshoppingCart cart);
    Task UpdateAsync(TshoppingCart cart);
    Task<TcartItem...> FindCartItemAsync(int cartId, int productId);
    Task AddCartItemAsync(TcartItem item);
    Task RemoveCartItemAsync(TcartItem item);
    Task ClearCartItemsAsync(int cartId);
}

// File: Sobee.Domain/Repositories/IOrderRepository.cs
public interface IOrderRepository
{
    Task<Torder...> GetByIdAsync(int orderId);
    Task<Torder...> GetByIdWithItemsAsync(int orderId);
    Task<PagedResult<Torder>> GetByUserIdAsync(string userId, int page, int pageSize);
    Task<Torder> CreateAsync(Torder order);
    Task UpdateAsync(Torder order);
    Task AddOrderItemsAsync(IEnumerable<TorderItem> items);
}

// File: Sobee.Domain/Repositories/IProductRepository.cs
public interface IProductRepository
{
    Task<Tproduct...> GetByIdAsync(int productId, bool includeImages = false);
    Task<PagedResult<Tproduct>> SearchAsync(ProductQueryParams query);
    Task<Tproduct> CreateAsync(Tproduct product);
    Task UpdateAsync(Tproduct product);
    Task DeleteAsync(Tproduct product);
    Task<bool> ExistsAsync(int productId);
}
5.2 DI Registration Pattern

// In Program.cs or an extension method

// Services
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IFavoriteService, FavoriteService>();
builder.Services.AddScoped<IPromoService, PromoService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();

// Repositories
builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
builder.Services.AddScoped<IPromoRepository, PromoRepository>();
5.3 Cross-Cutting Concerns
| Concern | Recommended Location |
| --- | --- |
| Input Validation | FluentValidation (current approach is good) |
| Business Rule Validation | Service layer (e.g., stock checks, promo eligibility) |
| Logging | Middleware (current Serilog approach) + Service layer for domain events |
| Caching | Repository layer decorators or service layer |
| Transaction Management | Service layer using IDbContextTransaction or Unit of Work |
| Authorization Policies | Controller attributes + Service layer checks for resource ownership |
| Error Mapping | ApiControllerBase (current approach is good) |

5.4 DTO Mapping Recommendation
Add AutoMapper for consistent, centralized mapping:


// File: sobee_API/Mapping/CartProfile.cs
public class CartProfile : Profile
{
    public CartProfile()
    {
        CreateMap<TshoppingCart, CartResponseDto>()
            .ForMember(d => d.Owner, o => o.MapFrom(s => s.UserId != null ... "user" : "guest"))
            .ForMember(d => d.Items, o => o.MapFrom(s => s.TcartItems));
        
        CreateMap<TcartItem, CartItemResponseDto>()
            .ForMember(d => d.LineTotal, o => o.MapFrom(s => (s.IntQuantity ...... 0) * (s.IntProduct.DecPrice)));
    }
}
## 6) Remediation Plan

## Phase 1: Foundation (Week 1-2)

Step 1.1: Create Service Interfaces
What: Define interfaces for all domain services

Files to Create:

sobee_API/Services/Interfaces/ICartService.cs
sobee_API/Services/Interfaces/IOrderService.cs
sobee_API/Services/Interfaces/IProductService.cs
sobee_API/Services/Interfaces/IReviewService.cs
sobee_API/Services/Interfaces/IFavoriteService.cs
sobee_API/Services/Interfaces/IPromoService.cs
sobee_API/Services/Interfaces/IAnalyticsService.cs
Acceptance Criteria:

 All interfaces defined with async methods
 No HTTP types (HttpContext, IActionResult) in interfaces
 Service methods return domain DTOs or Result<T> types
Step 1.2: Create Repository Interfaces
What: Define interfaces for data access

Files to Create:

Sobee.Domain/Repositories/ICartRepository.cs
Sobee.Domain/Repositories/IOrderRepository.cs
Sobee.Domain/Repositories/IProductRepository.cs
Sobee.Domain/Repositories/IPromoRepository.cs
Sobee.Domain/Repositories/IReviewRepository.cs
Acceptance Criteria:

 Repository interfaces in Domain project
 Methods return entities or queryables
 No business logic in interface signatures
## Phase 2: Cart Subsystem (Week 2-3) - CRITICAL

Step 2.1: Extract CartRepository
What: Move all cart EF queries to repository

Files to Create: Sobee.Domain/Repositories/CartRepository.cs

Files to Modify: None (new implementation)

Acceptance Criteria:

 FindByUserIdAsync, FindBySessionIdAsync implemented
 AddCartItemAsync, UpdateCartItemAsync, RemoveCartItemAsync implemented
 Cart merge queries extracted
 Unit tests for repository methods
Step 2.2: Extract CartService
What: Move cart business logic from controller to service

Files to Create: sobee_API/Services/CartService.cs

Logic to Move:

GetOrCreateCartAsync (merge logic)
ProjectCartAsync (DTO mapping)
Stock validation logic
Promo calculation logic
Acceptance Criteria:

 All cart business logic in service
 Service injected into controller
 Controller methods reduced to ~10 lines each
 Unit tests for service methods
Step 2.3: Refactor CartController
What: Simplify controller to use service

Files to Modify: sobee_API/Controllers/CartController.cs

Acceptance Criteria:

 No _db field in controller
 All methods call ICartService
 Controller only handles routing, validation, HTTP response translation
 Integration tests still pass
## Phase 3: Order/Checkout Subsystem (Week 3-4) - CRITICAL

Step 3.1: Extract OrderRepository
What: Move all order EF queries to repository

Files to Create: Sobee.Domain/Repositories/OrderRepository.cs

Acceptance Criteria:

 All order queries extracted
 Promo usage queries extracted
 Unit tests for repository methods
Step 3.2: Extract OrderService
What: Move checkout workflow to service

Files to Create: sobee_API/Services/OrderService.cs

Logic to Move:

Entire Checkout workflow (170 lines)
PayOrder transaction logic
CancelOrder logic
UpdateOrderStatus logic
Status transition validation
Acceptance Criteria:

 Transaction management in service layer
 Stock decrement logic centralized
 Promo snapshot logic centralized
 Service returns Result<OrderDto> for error handling
Step 3.3: Extract InventoryService
What: Centralize stock management

Files to Create: sobee_API/Services/InventoryService.cs

Acceptance Criteria:

 ValidateStockAsync(productId, quantity) method
 DecrementStockAsync(productId, quantity) method
 Used by both CartService and OrderService
Step 3.4: Refactor OrdersController
What: Simplify controller

Files to Modify: sobee_API/Controllers/OrdersController.cs

Acceptance Criteria:

 No _db field
 No transaction management code
 No business logic
 Integration tests still pass
## Phase 4: Product Subsystem (Week 4-5)

Step 4.1: Extract ProductRepository
Files to Create: Sobee.Domain/Repositories/ProductRepository.cs

Step 4.2: Extract ProductService
Files to Create: sobee_API/Services/ProductService.cs

Step 4.3: Refactor ProductsController
Files to Modify: sobee_API/Controllers/ProductsController.cs

Acceptance Criteria: Same pattern as Cart/Orders

## Phase 5: Secondary Subsystems (Week 5-6)

Step 5.1: Reviews & Favorites
Extract ReviewService, FavoriteService
Extract repositories
Refactor controllers
Step 5.2: Admin Subsystems
Extract PromoService, AnalyticsService
Extract repositories
Refactor admin controllers
## Phase 6: Polish & Testing (Week 6-7)

Step 6.1: Add AutoMapper
What: Centralize DTO mapping

Files to Create:

sobee_API/Mapping/CartProfile.cs
sobee_API/Mapping/OrderProfile.cs
sobee_API/Mapping/ProductProfile.cs
Step 6.2: Add Unit Tests
What: Test services and repositories in isolation

Files to Create:

sobee_API.Tests/Services/CartServiceTests.cs
sobee_API.Tests/Services/OrderServiceTests.cs
etc.
Acceptance Criteria:

 80%+ code coverage on services
 Repository mocks working
 No database required for unit tests
Step 6.3: Update Integration Tests
What: Ensure existing tests still pass

Files to Modify: Existing test files

## 7) Quick Win Checklist

These changes yield immediate improvement with minimal risk:

 1. Create ICartService interface - Define the contract without changing implementation yet. Unblocks parallel work.

 2. Extract StockValidationHelper - Move the 4 lines of stock validation to a static helper (does not require full refactor).


public static class StockValidationHelper
{
    public static (bool IsValid, int Available) ValidateStock(Tproduct product, int requested)
        => (requested <= product.IntStockAmount, product.IntStockAmount);
}
 3. Extract PromoCalculationHelper - Move discount calculation (5 lines) to static helper to eliminate duplication.

 4. Add Result<T> type - Create a simple result wrapper for service return types to handle errors without exceptions.


public record ServiceResult<T>(T... Value, string... ErrorCode, string... ErrorMessage)
{
    public bool IsSuccess => ErrorCode == null;
    public static ServiceResult<T> Ok(T value) => new(value, null, null);
    public static ServiceResult<T> Fail(string code, string message) => new(default, code, message);
}
 5. Document current architecture - Add a CLAUDE.md or ARCHITECTURE.md explaining the current state and target state.

 6. Add // TODO: Extract to service comments - Mark all business logic blocks for future extraction. Helps developers know what to avoid touching.

 7. Create service folder structure - Add empty folders/files as scaffolding:


sobee_API/Services/
|-- Interfaces/
|   |-- ICartService.cs
|   |-- IOrderService.cs
|   \-- ...
|-- CartService.cs (placeholder)
\-- OrderService.cs (placeholder)
### Summary

This codebase has a significant technical debt in its architecture. The current pattern treats controllers as "fat controllers" that perform all operations from HTTP handling to database access. This is common in early-stage projects but becomes a liability as the codebase grows.

The recommended approach is an incremental refactor starting with the highest-risk areas (Cart and Orders) and progressively extracting services and repositories. The quick wins can be implemented immediately to reduce code duplication while the larger refactor is planned and executed in sprints.

Can you export this into a txt file or did you already do that
I haven't exported it yet. Let me create the file now.

Write c:\Users\Eyass\source\repos\sobee_SPA\sobee_API\ARCHITECTURE_AUDIT_REPORT.md
889 lines
# Layered Architecture Audit Report

## Sobee E-Commerce API (ASP.NET Core 8.0)

**Audit Date:** 2026-01-22
**Auditor:** Claude (Senior .NET Architect)

---

## 1) Executive Summary

### Overall Compliance: **LOW**

The codebase follows a **monolithic "Active Record" / "Transaction Script"** pattern rather than the expected **Controller-Service-Repository** layered architecture. Controllers directly inject and use `DbContext` for all data access and contain extensive business logic inline.

### Biggest Risks (Top 5)

| Rank | Risk | Impact |
|------|------|--------|
| 1 | **All 14 domain controllers inject DbContext directly** | Untestable, tightly coupled, no abstraction layer |
| 2 | **Business logic (pricing, discounts, stock, checkout workflow) embedded in controllers** | Duplicated logic, error-prone changes, no reuse |
| 3 | **No service layer for domain operations** | Cannot unit test business rules without spinning up database |
| 4 | **No repository abstraction** | EF queries scattered across controllers, difficult to optimize |
| 5 | **Transaction management in controllers** | Inconsistent transactional boundaries, risk of partial commits |

### Estimated Effort by Subsystem

| Subsystem | Violation Count | Effort | Priority |
|-----------|-----------------|--------|----------|
| **Cart** | 9 EF queries in controller | **L** | Critical |
| **Orders/Checkout** | 12+ EF queries, transaction logic in controller | **L** | Critical |
| **Products** | 8 EF queries in controller | **M** | High |
| **Reviews** | 6 EF queries in controller | **M** | Medium |
| **Favorites** | 4 EF queries in controller | **S** | Low |
| **Admin Analytics** | 15+ complex EF queries | **L** | Medium |
| **Admin Promos** | 6 EF queries in controller | **M** | Low |
| **Admin Users** | 5 EF queries in controller | **M** | Low |
| **Auth/Users** | UserManager only (acceptable) | **S** | Low |
| **Payments** | 1 EF query | **S** | Low |

**Legend:** S = Small (<2 days), M = Medium (2-5 days), L = Large (5+ days)

---

## 2) Architecture Map

### Projects/Assemblies

| Project | Role | Dependencies |
|---------|------|--------------|
| `sobee_API` | **Presentation Layer** (Controllers, DTOs, Validators, Middleware) | Sobee.Domain |
| `Sobee.Domain` | **Data Access + Domain Entities** (DbContexts, Entities) | EF Core, Identity |
| `sobee_API.Tests` | Integration Tests | sobee_API |

### Current Architecture Diagram

```
+------------------------------------------------------------------+
|                     PRESENTATION LAYER                            |
|                        (sobee_API)                                |
+------------------------------------------------------------------+
|  Controllers (14)          |  DTOs (23+)    |  Validators (13+)   |
|  +- CartController         |  +- Cart/*     |  FluentValidation   |
|  +- OrdersController       |  +- Orders/*   |                     |
|  +- ProductsController     |  +- Products/* |                     |
|  +- ReviewsController      |  +- ...        |                     |
|  +- ...                    |                |                     |
+----------------------------+----------------+---------------------+
                              |
                    DIRECT DbContext INJECTION
                    (NO SERVICE/REPOSITORY)
+------------------------------------------------------------------+
|                                                                   |
|  Services (4)                                                     |
|  +- GuestSessionService      (utility, not domain service)        |
|  +- RequestIdentityResolver  (utility, not domain service)        |
|  +- IdentitySeedService      (startup seed)                       |
|  +- RoleSeedService          (startup seed)                       |
|                                                                   |
+------------------------------------------------------------------+
                              |
+------------------------------------------------------------------+
|                      DATA ACCESS LAYER                            |
|                      (Sobee.Domain)                               |
+------------------------------------------------------------------+
|  DbContexts                 |  Entities                           |
|  +- SobeecoredbContext      |  +- Cart (TshoppingCart, TcartItem) |
|  +- ApplicationDbContext    |  +- Orders (Torder, TorderItem)     |
|                             |  +- Products (Tproduct, etc.)       |
|                             |  +- Payments, Reviews, etc.         |
|                             |  +- Identity (ApplicationUser)      |
+------------------------------------------------------------------+
                              |
                    +-----------------+
                    |   SQL Server    |
                    +-----------------+
```

### Layer Violations Summary

| Violation Type | Count | Examples |
|----------------|-------|----------|
| Controller -> DbContext direct injection | **14** | All domain controllers |
| Business logic in controllers | **~50 methods** | Checkout, AddItem, ApplyPromo |
| EF LINQ queries in controllers | **60+** | Throughout all controllers |
| Transaction management in controllers | **3** | OrdersController (Checkout, Pay, etc.) |
| Missing service layer | **100%** | No domain services exist |
| Missing repository layer | **100%** | No repositories exist |

### Circular Dependencies

None detected. The dependency flow is strictly: `sobee_API` -> `Sobee.Domain`.

---

## 3) Findings (with Evidence)

### LAYER-001: Controllers Inject DbContext Directly (Blocker)

**Severity:** Blocker
**Rule Violated:** Controllers must NOT use DbContext directly
**Files Affected:**

| Controller | File Path |
|------------|-----------|
| CartController | sobee_API/Controllers/CartController.cs:18 |
| OrdersController | sobee_API/Controllers/OrdersController.cs:24 |
| ProductsController | sobee_API/Controllers/ProductsController.cs:15 |
| ReviewsController | sobee_API/Controllers/ReviewsController.cs:15 |
| FavoritesController | sobee_API/Controllers/FavoritesController.cs:17 |
| PaymentMethodsController | sobee_API/Controllers/PaymentMethodsController.cs:13 |
| AdminAnalyticsController | sobee_API/Controllers/AdminAnalyticsController.cs:15 |
| AdminPromosController | sobee_API/Controllers/AdminPromosController.cs:17 |
| AdminUsersController | sobee_API/Controllers/AdminUsersController.cs:17 |

**Code Excerpt (CartController):**
```csharp
public class CartController : ApiControllerBase
{
    private readonly SobeecoredbContext _db;  // <-- VIOLATION

    public CartController(SobeecoredbContext db, ...)
    {
        _db = db;  // Direct injection
    }
```

**Why It's a Problem:**
- Cannot unit test controller logic without database
- Tight coupling makes refactoring dangerous
- No abstraction for query optimization
- Violates Single Responsibility Principle

- ---

### LAYER-002: Business Logic in Controllers (Blocker)

**Severity:** Blocker
**Rule Violated:** Business workflows must be in service layer
**Files Affected:** CartController, OrdersController

**Example 1: Cart Merge Logic** (CartController.cs:364-430)
```csharp
private async Task<TshoppingCart> GetOrCreateCartAsync(...)
{
    // 60+ lines of cart creation, merging, session rotation logic
    if (!string.IsNullOrWhiteSpace(userId) && sessionCart != null && canMergeGuestSession)
    {
        if (userCart == null)
        {
            sessionCart.UserId = userId;
            sessionCart.SessionId = null;
            // ... business logic for claiming cart
        }
        // Merge items: add quantities into userCart
        foreach (var sessionItem in sessionCart.TcartItems.ToList())
        {
            // ... merge logic
        }
    }
}
```

**Example 2: Checkout Workflow** (OrdersController.cs:110-279)
```csharp
public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request)
{
    // 170 lines of:
    // - Cart validation
    // - Subtotal calculation
    // - Promo discount calculation
    // - Stock validation and decrement
    // - Order creation
    // - Order items creation
    // - Cart clearing
    // - Transaction management
}
```

**Example 3: Promo Discount Calculation** (CartController.cs:492-505)
```csharp
var discountAmount = 0m;
if (promo.DiscountPercentage > 0 && subtotal > 0)
{
    discountAmount = subtotal * (promo.DiscountPercentage / 100m);
}
var total = subtotal - discountAmount;
if (total < 0) total = 0;
```

**Why It's a Problem:**
- Business rules are duplicated (discount calculation appears in Cart AND Orders)
- Cannot reuse checkout logic from other entry points
- Unit testing requires HTTP context mocking
- Changes to pricing rules require modifying controllers

- ---

### LAYER-003: EF Queries Scattered in Controllers (Major)

**Severity:** Major
**Rule Violated:** Data access must be isolated in repositories
**Files Affected:** All domain controllers

**Examples:**

**CartController** - 9+ distinct EF queries:
// Line 61-62
var product = await _db.Tproducts
    .FirstOrDefaultAsync(p => p.IntProductId == request.ProductId);

// Line 77-79
var existingItem = await _db.TcartItems.FirstOrDefaultAsync(i =>
    i.IntShoppingCartId == cart.IntShoppingCartId &&
    i.IntProductId == request.ProductId);

// Line 132-134
var promo = await _db.Tpromotions.FirstOrDefaultAsync(p =>
    p.StrPromoCode == promoCode &&
    p.DtmExpirationDate > DateTime.UtcNow);

// Line 529-541 (complex join)
var promo = await _db.TpromoCodeUsageHistories
    .Join(_db.Tpromotions,
        usage => usage.PromoCode,
        promo => promo.StrPromoCode,
        (usage, promo) => new { usage, promo })
    .Where(x => x.usage.IntShoppingCartId == cartId && ...)

**AdminAnalyticsController** - 15+ complex analytical queries spanning 600+ lines

**Why It's a Problem:**
- Query optimization is scattered and hard to audit
- Duplicate query patterns across controllers
- Cannot easily add caching layer
- Difficult to profile database performance

- ---

### LAYER-004: Transaction Management in Controllers (Major)

**Severity:** Major
**Rule Violated:** Transactional boundaries should be managed by service layer
**Files Affected:** OrdersController.cs

**Checkout Transaction** (Lines 182-278):
```csharp
using var tx = await _db.Database.BeginTransactionAsync();
try
{
    // 1) Validate stock and decrement it
    foreach (var cartItem in cart.TcartItems)
    {
        if (product.IntStockAmount < qty)
            return ConflictError(...);  // <-- Returns from inside transaction
        product.IntStockAmount -= qty;
    }
    // 2) Create order header
    // 3) Create order items
    // 4) Clear cart items
    // 5) Clear applied promos
    await _db.SaveChangesAsync();
    await tx.CommitAsync();
}
catch
{
    await tx.RollbackAsync();
    return ServerError("Checkout failed.");
}
```

**PayOrder Transaction** (Lines 377-406):
```csharp
using var tx = await _db.Database.BeginTransactionAsync();
try
{
    var payment = new Tpayment { ... };
    _db.Tpayments.Add(payment);
    order.IntPaymentMethodId = paymentMethod.IntPaymentMethodId;
    order.StrOrderStatus = targetStatus;
    await _db.SaveChangesAsync();
    await tx.CommitAsync();
}
catch
{
    await tx.RollbackAsync();
    return ServerError("Payment failed.");
}
```

**Why It's a Problem:**
- Transaction logic mixed with HTTP response logic
- Early returns from transaction blocks can lead to leaks if not careful
- Cannot reuse transactional workflow from other contexts
- Difficult to test transaction rollback scenarios

- ---

### LAYER-005: Stock Validation Logic in Controller (Major)

**Severity:** Major
**Rule Violated:** Inventory rules are business logic, not presentation logic
**Files Affected:** CartController, OrdersController

**CartController** (Lines 84-103):
// Stock check for new item
if (request.Quantity > product.IntStockAmount)
    return StockConflict(product.IntProductId, product.IntStockAmount, request.Quantity);

// Stock check for increment
if (newQuantity > product.IntStockAmount)
    return StockConflict(product.IntProductId, product.IntStockAmount, newQuantity);

**OrdersController** (Lines 186-207):
foreach (var cartItem in cart.TcartItems)
{
    if (product.IntStockAmount < qty)
    {
        return ConflictError("Insufficient stock.", ...);
    }
    product.IntStockAmount -= qty;  // Inventory modification
}

**Why It's a Problem:**
- Stock rules duplicated across Cart and Orders
- Cannot enforce stock policies uniformly
- Race conditions possible without proper service-level locking

- ---

### LAYER-006: Order Status Transition Logic in Controller (Major)

**Severity:** Major
**Rule Violated:** Status workflow rules belong in service/domain layer
**File:** OrdersController.cs:305-320, 418-468

var currentStatus = OrderStatuses.Normalize(order.StrOrderStatus);
if (!OrderStatuses.IsCancellable(currentStatus))
{
    return ConflictError("Order cannot be cancelled...");
}

if (!OrderStatuses.CanTransition(currentStatus, targetStatus))
{
    return ConflictError("Invalid status transition.", ...);
}

// Status change logic
order.StrOrderStatus = newStatus;
if (string.Equals(newStatus, OrderStatuses.Shipped, ...))
{
    order.DtmShippedDate = DateTime.UtcNow;
}

**Why It's a Problem:**
- State machine logic in controller
- Workflow rules tied to HTTP layer
- Cannot trigger status changes from background jobs easily

- ---

### LAYER-007: DTO Projection in Controllers (Minor)

**Severity:** Minor
**Rule Violated:** DTO mapping should be centralized
**Files:** All controllers

**Example** (CartController Lines 473-527):
private async Task<CartResponseDto> ProjectCartAsync(TshoppingCart cart, ...)
{
    var items = cart.TcartItems.Select(i => new CartItemResponseDto
    {
        CartItemId = i.IntCartItemId,
        ProductId = i.IntProductId,
        // ... 15+ property mappings
    }).ToList();

    return new CartResponseDto
    {
        CartId = cart.IntShoppingCartId,
        // ... more mappings
    };
}

**Why It's a Problem:**
- Mapping logic scattered across controllers
- No centralized mapping configuration (like AutoMapper profiles)
- Hard to maintain consistency across endpoints

- ---

### LAYER-008: Services Access HTTP Types Correctly (Positive Finding)

**Severity:** N/A - Compliant
**Note:** `GuestSessionService` and `RequestIdentityResolver` do accept `HttpRequest`/`HttpResponse` parameters, but these are **infrastructure services**, not domain services. This is an acceptable pattern for cross-cutting concerns like session management.

---

## 4) Inventory of Endpoints

### Cart Endpoints

| Route | Controller Action | Service Method | Repository Method |
|-------|-------------------|----------------|-------------------|
| GET /api/cart | CartController.GetCart() | **DIRECT DB ACCESS** | N/A |
| POST /api/cart/items | CartController.AddItem() | **DIRECT DB ACCESS** | N/A |
| PUT /api/cart/items/{id} | CartController.UpdateItem() | **DIRECT DB ACCESS** | N/A |
| DELETE /api/cart/items/{id} | CartController.RemoveItem() | **DIRECT DB ACCESS** | N/A |
| DELETE /api/cart | CartController.ClearCart() | **DIRECT DB ACCESS** | N/A |
| POST /api/cart/promo/apply | CartController.ApplyPromo() | **DIRECT DB ACCESS** | N/A |
| DELETE /api/cart/promo | CartController.RemovePromo() | **DIRECT DB ACCESS** | N/A |

### Order Endpoints

| Route | Controller Action | Service Method | Repository Method |
|-------|-------------------|----------------|-------------------|
| GET /api/orders/{id} | OrdersController.GetOrder() | **DIRECT DB ACCESS** | N/A |
| GET /api/orders/my | OrdersController.GetMyOrders() | **DIRECT DB ACCESS** | N/A |
| POST /api/orders/checkout | OrdersController.Checkout() | **DIRECT DB ACCESS** | N/A |
| POST /api/orders/{id}/cancel | OrdersController.CancelOrder() | **DIRECT DB ACCESS** | N/A |
| POST /api/orders/{id}/pay | OrdersController.PayOrder() | **DIRECT DB ACCESS** | N/A |
| PATCH /api/orders/{id}/status | OrdersController.UpdateOrderStatus() | **DIRECT DB ACCESS** | N/A |

### Product Endpoints

| Route | Controller Action | Service Method | Repository Method |
|-------|-------------------|----------------|-------------------|
| GET /api/products | ProductsController.GetProducts() | **DIRECT DB ACCESS** | N/A |
| GET /api/products/{id} | ProductsController.GetProduct() | **DIRECT DB ACCESS** | N/A |
| POST /api/products | ProductsController.CreateProduct() | **DIRECT DB ACCESS** | N/A |
| PUT /api/products/{id} | ProductsController.UpdateProduct() | **DIRECT DB ACCESS** | N/A |
| DELETE /api/products/{id} | ProductsController.DeleteProduct() | **DIRECT DB ACCESS** | N/A |
| POST /api/products/{id}/images | ProductsController.AddProductImage() | **DIRECT DB ACCESS** | N/A |
| DELETE /api/products/{id}/images/{imgId} | ProductsController.DeleteProductImage() | **DIRECT DB ACCESS** | N/A |

### Review Endpoints

| Route | Controller Action | Service Method | Repository Method |
|-------|-------------------|----------------|-------------------|
| GET /api/reviews/product/{id} | ReviewsController.GetByProduct() | **DIRECT DB ACCESS** | N/A |
| POST /api/reviews/product/{id} | ReviewsController.Create() | **DIRECT DB ACCESS** | N/A |
| POST /api/reviews/{id}/reply | ReviewsController.Reply() | **DIRECT DB ACCESS** | N/A |
| DELETE /api/reviews/{id} | ReviewsController.DeleteReview() | **DIRECT DB ACCESS** | N/A |
| DELETE /api/reviews/replies/{id} | ReviewsController.DeleteReply() | **DIRECT DB ACCESS** | N/A |

### Favorites Endpoints

| Route | Controller Action | Service Method | Repository Method |
|-------|-------------------|----------------|-------------------|
| GET /api/favorites | FavoritesController.GetMyFavorites() | **DIRECT DB ACCESS** | N/A |
| POST /api/favorites/{productId} | FavoritesController.AddFavorite() | **DIRECT DB ACCESS** | N/A |
| DELETE /api/favorites/{productId} | FavoritesController.RemoveFavorite() | **DIRECT DB ACCESS** | N/A |

### Admin Analytics (all DIRECT DB ACCESS)

| Route | Controller Action |
|-------|-------------------|
| GET /api/admin/analytics/revenue | GetRevenueByPeriod() |
| GET /api/admin/analytics/orders/status | GetOrderStatusBreakdown() |
| GET /api/admin/analytics/reviews/* | 4 endpoints |
| GET /api/admin/analytics/products/* | 2 endpoints |
| GET /api/admin/analytics/inventory/summary | GetInventorySummary() |
| GET /api/admin/analytics/orders/fulfillment | GetFulfillmentMetrics() |
| GET /api/admin/analytics/customers/* | 4 endpoints |
| GET /api/admin/analytics/wishlist/top | GetMostWishlisted() |

### Admin Promos (all DIRECT DB ACCESS)

| Route | Controller Action |
|-------|-------------------|
| GET /api/admin/promos | GetPromos() |
| POST /api/admin/promos | CreatePromo() |
| PUT /api/admin/promos/{id} | UpdatePromo() |
| DELETE /api/admin/promos/{id} | DeletePromo() |

### Admin Users (all DIRECT DB ACCESS via ApplicationDbContext)

| Route | Controller Action |
|-------|-------------------|
| GET /api/admin/users | GetUsers() |
| PUT /api/admin/users/{id}/admin | UpdateAdminRole() |

### Auth/Users (UserManager - acceptable)

| Route | Controller Action | Service |
|-------|-------------------|---------|
| POST /api/auth/register | AuthController.Register() | UserManager OK |
| GET /api/users/profile | UsersController.GetProfile() | UserManager OK |
| PUT /api/users/profile | UsersController.UpdateProfile() | UserManager OK |
| PUT /api/users/password | UsersController.UpdatePassword() | UserManager OK |

### Other

| Route | Controller Action | Service |
|-------|-------------------|---------|
| GET /api/me | MeController.Get() | ClaimsPrincipal OK |
| GET /api/paymentmethods | PaymentMethodsController.GetPaymentMethods() | **DIRECT DB ACCESS** |

---

## 5) Recommendations (Target Patterns)

### 5.1 Standard Interface Patterns

**Services (Application Layer):**
// File: sobee_API/Services/ICartService.cs
public interface ICartService
{
    Task<CartDto> GetCartAsync(string... userId, string... sessionId);
    Task<CartDto> AddItemAsync(string... userId, string... sessionId, int productId, int quantity);
    Task<CartDto> UpdateItemAsync(string... userId, string... sessionId, int cartItemId, int quantity);
    Task<CartDto> RemoveItemAsync(string... userId, string... sessionId, int cartItemId);
    Task<CartDto> ClearCartAsync(string... userId, string... sessionId);
    Task<PromoResult> ApplyPromoAsync(string... userId, string... sessionId, string promoCode);
    Task<CartDto> RemovePromoAsync(string... userId, string... sessionId);
}

// File: sobee_API/Services/IOrderService.cs
public interface IOrderService
{
    Task<OrderDto> GetOrderAsync(string... userId, string... sessionId, int orderId);
    Task<PagedResult<OrderDto>> GetUserOrdersAsync(string userId, int page, int pageSize);
    Task<OrderDto> CheckoutAsync(string... userId, string... sessionId, CheckoutRequest request);
    Task<OrderDto> CancelOrderAsync(string... userId, string... sessionId, int orderId);
    Task<OrderDto> PayOrderAsync(string... userId, string... sessionId, int orderId, PayOrderRequest request);
    Task<OrderDto> UpdateStatusAsync(int orderId, string newStatus); // Admin
}

// File: sobee_API/Services/IProductService.cs
public interface IProductService
{
    Task<PagedResult<ProductListDto>> GetProductsAsync(ProductQueryParams query, bool isAdmin);
    Task<ProductDetailDto> GetProductAsync(int productId, bool isAdmin);
    Task<ProductDetailDto> CreateProductAsync(CreateProductRequest request);
    Task<ProductDetailDto> UpdateProductAsync(int productId, UpdateProductRequest request);
    Task DeleteProductAsync(int productId);
}

**Repositories (Data Layer):**
// File: Sobee.Domain/Repositories/ICartRepository.cs
public interface ICartRepository
{
    Task<TshoppingCart...> FindByUserIdAsync(string userId);
    Task<TshoppingCart...> FindBySessionIdAsync(string sessionId);
    Task<TshoppingCart> CreateAsync(TshoppingCart cart);
    Task UpdateAsync(TshoppingCart cart);
    Task<TcartItem...> FindCartItemAsync(int cartId, int productId);
    Task AddCartItemAsync(TcartItem item);
    Task RemoveCartItemAsync(TcartItem item);
    Task ClearCartItemsAsync(int cartId);
}

// File: Sobee.Domain/Repositories/IOrderRepository.cs
public interface IOrderRepository
{
    Task<Torder...> GetByIdAsync(int orderId);
    Task<Torder...> GetByIdWithItemsAsync(int orderId);
    Task<PagedResult<Torder>> GetByUserIdAsync(string userId, int page, int pageSize);
    Task<Torder> CreateAsync(Torder order);
    Task UpdateAsync(Torder order);
    Task AddOrderItemsAsync(IEnumerable<TorderItem> items);
}

// File: Sobee.Domain/Repositories/IProductRepository.cs
public interface IProductRepository
{
    Task<Tproduct...> GetByIdAsync(int productId, bool includeImages = false);
    Task<PagedResult<Tproduct>> SearchAsync(ProductQueryParams query);
    Task<Tproduct> CreateAsync(Tproduct product);
    Task UpdateAsync(Tproduct product);
    Task DeleteAsync(Tproduct product);
    Task<bool> ExistsAsync(int productId);
}

### 5.2 DI Registration Pattern

// In Program.cs or an extension method

// Services
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IFavoriteService, FavoriteService>();
builder.Services.AddScoped<IPromoService, PromoService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();

// Repositories
builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
builder.Services.AddScoped<IPromoRepository, PromoRepository>();

### 5.3 Cross-Cutting Concerns

| Concern | Recommended Location |
|---------|---------------------|
| **Input Validation** | FluentValidation (current approach is good) |
| **Business Rule Validation** | Service layer (e.g., stock checks, promo eligibility) |
| **Logging** | Middleware (current Serilog approach) + Service layer for domain events |
| **Caching** | Repository layer decorators or service layer |
| **Transaction Management** | Service layer using IDbContextTransaction or Unit of Work |
| **Authorization Policies** | Controller attributes + Service layer checks for resource ownership |
| **Error Mapping** | ApiControllerBase (current approach is good) |

### 5.4 DTO Mapping Recommendation

Add **AutoMapper** for consistent, centralized mapping:

// File: sobee_API/Mapping/CartProfile.cs
public class CartProfile : Profile
{
    public CartProfile()
    {
        CreateMap<TshoppingCart, CartResponseDto>()
            .ForMember(d => d.Owner, o => o.MapFrom(s => s.UserId != null ... "user" : "guest"))
            .ForMember(d => d.Items, o => o.MapFrom(s => s.TcartItems));

        CreateMap<TcartItem, CartItemResponseDto>()
            .ForMember(d => d.LineTotal, o => o.MapFrom(s => (s.IntQuantity ...... 0) * (s.IntProduct.DecPrice)));
    }
}

---

## 6) Remediation Plan

### Phase 1: Foundation (Week 1-2)

#### Step 1.1: Create Service Interfaces

**What:** Define interfaces for all domain services
**Files to Create:**
- sobee_API/Services/Interfaces/ICartService.cs
- sobee_API/Services/Interfaces/IOrderService.cs
- sobee_API/Services/Interfaces/IProductService.cs
- sobee_API/Services/Interfaces/IReviewService.cs
- sobee_API/Services/Interfaces/IFavoriteService.cs
- sobee_API/Services/Interfaces/IPromoService.cs
- sobee_API/Services/Interfaces/IAnalyticsService.cs

**Acceptance Criteria:**
- [ ] All interfaces defined with async methods
- [ ] No HTTP types (HttpContext, IActionResult) in interfaces
- [ ] Service methods return domain DTOs or Result<T> types

#### Step 1.2: Create Repository Interfaces

**What:** Define interfaces for data access
**Files to Create:**
- Sobee.Domain/Repositories/ICartRepository.cs
- Sobee.Domain/Repositories/IOrderRepository.cs
- Sobee.Domain/Repositories/IProductRepository.cs
- Sobee.Domain/Repositories/IPromoRepository.cs
- Sobee.Domain/Repositories/IReviewRepository.cs

**Acceptance Criteria:**
- [ ] Repository interfaces in Domain project
- [ ] Methods return entities or queryables
- [ ] No business logic in interface signatures

### Phase 2: Cart Subsystem (Week 2-3) - CRITICAL

#### Step 2.1: Extract CartRepository

**What:** Move all cart EF queries to repository
**Files to Create:** Sobee.Domain/Repositories/CartRepository.cs
**Files to Modify:** None (new implementation)

**Acceptance Criteria:**
- [ ] FindByUserIdAsync, FindBySessionIdAsync implemented
- [ ] AddCartItemAsync, UpdateCartItemAsync, RemoveCartItemAsync implemented
- [ ] Cart merge queries extracted
- [ ] Unit tests for repository methods

#### Step 2.2: Extract CartService

**What:** Move cart business logic from controller to service
**Files to Create:** sobee_API/Services/CartService.cs
**Logic to Move:**
- GetOrCreateCartAsync (merge logic)
- ProjectCartAsync (DTO mapping)
- Stock validation logic
- Promo calculation logic

**Acceptance Criteria:**
- [ ] All cart business logic in service
- [ ] Service injected into controller
- [ ] Controller methods reduced to ~10 lines each
- [ ] Unit tests for service methods

#### Step 2.3: Refactor CartController

**What:** Simplify controller to use service
**Files to Modify:** sobee_API/Controllers/CartController.cs

**Acceptance Criteria:**
- [ ] No _db field in controller
- [ ] All methods call ICartService
- [ ] Controller only handles routing, validation, HTTP response translation
- [ ] Integration tests still pass

### Phase 3: Order/Checkout Subsystem (Week 3-4) - CRITICAL

#### Step 3.1: Extract OrderRepository

**What:** Move all order EF queries to repository
**Files to Create:** Sobee.Domain/Repositories/OrderRepository.cs

**Acceptance Criteria:**
- [ ] All order queries extracted
- [ ] Promo usage queries extracted
- [ ] Unit tests for repository methods

#### Step 3.2: Extract OrderService

**What:** Move checkout workflow to service
**Files to Create:** sobee_API/Services/OrderService.cs
**Logic to Move:**
- Entire Checkout workflow (170 lines)
- PayOrder transaction logic
- CancelOrder logic
- UpdateOrderStatus logic
- Status transition validation

**Acceptance Criteria:**
- [ ] Transaction management in service layer
- [ ] Stock decrement logic centralized
- [ ] Promo snapshot logic centralized
- [ ] Service returns Result<OrderDto> for error handling

#### Step 3.3: Extract InventoryService

**What:** Centralize stock management
**Files to Create:** sobee_API/Services/InventoryService.cs

**Acceptance Criteria:**
- [ ] ValidateStockAsync(productId, quantity) method
- [ ] DecrementStockAsync(productId, quantity) method
- [ ] Used by both CartService and OrderService

#### Step 3.4: Refactor OrdersController

**What:** Simplify controller
**Files to Modify:** sobee_API/Controllers/OrdersController.cs

**Acceptance Criteria:**
- [ ] No _db field
- [ ] No transaction management code
- [ ] No business logic
- [ ] Integration tests still pass

### Phase 4: Product Subsystem (Week 4-5)

#### Step 4.1: Extract ProductRepository

**Files to Create:** Sobee.Domain/Repositories/ProductRepository.cs

#### Step 4.2: Extract ProductService

**Files to Create:** sobee_API/Services/ProductService.cs

#### Step 4.3: Refactor ProductsController

**Files to Modify:** sobee_API/Controllers/ProductsController.cs

**Acceptance Criteria:** Same pattern as Cart/Orders

### Phase 5: Secondary Subsystems (Week 5-6)

#### Step 5.1: Reviews & Favorites

- Extract ReviewService, FavoriteService
- Extract repositories
- Refactor controllers

#### Step 5.2: Admin Subsystems

- Extract PromoService, AnalyticsService
- Extract repositories
- Refactor admin controllers

### Phase 6: Polish & Testing (Week 6-7)

#### Step 6.1: Add AutoMapper

**What:** Centralize DTO mapping
**Files to Create:**
- sobee_API/Mapping/CartProfile.cs
- sobee_API/Mapping/OrderProfile.cs
- sobee_API/Mapping/ProductProfile.cs

#### Step 6.2: Add Unit Tests

**What:** Test services and repositories in isolation
**Files to Create:**
- sobee_API.Tests/Services/CartServiceTests.cs
- sobee_API.Tests/Services/OrderServiceTests.cs
- etc.

**Acceptance Criteria:**
- [ ] 80%+ code coverage on services
- [ ] Repository mocks working
- [ ] No database required for unit tests

#### Step 6.3: Update Integration Tests

**What:** Ensure existing tests still pass
**Files to Modify:** Existing test files

---

## 7) Quick Win Checklist

These changes yield immediate improvement with minimal risk:

- [ ] **1. Create ICartService interface** - Define the contract without changing implementation yet. Unblocks parallel work.

- [ ] **2. Extract StockValidationHelper** - Move the 4 lines of stock validation to a static helper (does not require full refactor).
  public static class StockValidationHelper
  {
      public static (bool IsValid, int Available) ValidateStock(Tproduct product, int requested)
          => (requested <= product.IntStockAmount, product.IntStockAmount);
  }

- [ ] **3. Extract PromoCalculationHelper** - Move discount calculation (5 lines) to static helper to eliminate duplication.

- [ ] **4. Add Result<T> type** - Create a simple result wrapper for service return types to handle errors without exceptions.
  public record ServiceResult<T>(T... Value, string... ErrorCode, string... ErrorMessage)
  {
      public bool IsSuccess => ErrorCode == null;
      public static ServiceResult<T> Ok(T value) => new(value, null, null);
      public static ServiceResult<T> Fail(string code, string message) => new(default, code, message);
  }

- [ ] **5. Document current architecture** - Add a CLAUDE.md or ARCHITECTURE.md explaining the current state and target state.

- [ ] **6. Add "// TODO: Extract to service" comments** - Mark all business logic blocks for future extraction. Helps developers know what to avoid touching.

- [ ] **7. Create service folder structure** - Add empty folders/files as scaffolding:
  ```
  sobee_API/Services/
  +-- Interfaces/
  |   +-- ICartService.cs
  |   +-- IOrderService.cs
  |   +-- ...
  +-- CartService.cs (placeholder)
  +-- OrderService.cs (placeholder)
  ```

---

## Summary

This codebase has a significant **technical debt** in its architecture. The current pattern treats controllers as "fat controllers" that perform all operations from HTTP handling to database access. This is common in early-stage projects but becomes a liability as the codebase grows.

The recommended approach is an **incremental refactor** starting with the highest-risk areas (Cart and Orders) and progressively extracting services and repositories. The quick wins can be implemented immediately to reduce code duplication while the larger refactor is planned and executed in sprints.

---

**Report Generated:** 2026-01-22
**Auditor:** Claude (Senior .NET Architect)
Done. The report has been exported to:

ARCHITECTURE_AUDIT_REPORT.md

The file is located in your sobee_API solution folder and contains the complete audit with all 7 sections:

### Executive Summary

Architecture Map
Findings (with evidence)
Inventory of Endpoints
Recommendations (target patterns)
Remediation Plan
Quick Win Checklist
ROLE
You are a senior .NET architect and test-focused refactoring lead. Your task is to produce a phase-by-phase conversion plan to refactor a monolithic ASP.NET Core Web API into a Layered Architecture (Controller -> Service/Application -> Repository/Infrastructure), using the provided audit findings as the source of truth. You must incorporate testing as a gate after each phase, using the provided rubric.

INPUTS YOU MUST USE
## 1) The audit report (paste or read its contents). Treat its findings, endpoint inventory, and remediation suggestions as authoritative.

## 2) The testing rubric provided below (must be enforced as a gate to proceed per phase).

NON-NEGOTIABLE RULES
- Do NOT modify code. You are producing a plan only.
- Every phase must end with tests required by the rubric for that phase.
- "Tests pass" is the exit criterion for each phase before moving on.
- Keep behavior stable. If a phase may change behavior, you must explicitly call out the risk and how you will detect/regress it with tests.
- Be concrete: list specific folders/files/classes likely impacted and exact acceptance criteria.
- Use .NET best practices (DI, middleware, WebApplicationFactory, xUnit/NUnit, EF Core patterns).
- Recommend primary-source documentation where helpful (official Microsoft docs, EF Core docs, ASP.NET Core testing docs). Provide doc titles/keywords (and optionally links if available).

TARGET ARCHITECTURE (DEFINITION)
Layered monolith (not microservices):
- Presentation layer: Controllers/Minimal API endpoints.
  - Handles HTTP concerns only: routing, model binding, validation, auth attributes, mapping to response.
  - Must NOT: inject DbContext, query EF, run SQL, implement business workflows.
- Application/Service layer: Use cases / services.
  - Orchestrates workflows, enforces business rules, ownership/security checks, transactional boundaries (if appropriate).
  - Must NOT: reference ControllerBase/IActionResult/HttpContext.
- Infrastructure/Data access layer:
  - Repositories (or query layer) encapsulating EF Core/SQL.
  - Must NOT: contain business rules (pricing, discounts, state transitions) beyond query shaping.

OUTPUT FORMAT (REQUIRED)
Produce the plan with these sections:

## A) High-level roadmap (Phase list)

- Phase name, goal, affected subsystems (Cart, Orders, Products, Auth, etc.)
- "Why this phase first" (tie directly to audit severity & risk)

## B) Phase-by-phase plan (DETAILED)

For EACH phase, include:
### 1) Scope

   - Which controllers/endpoints are included in this phase (prefer vertical slices).
## 2) Refactor tasks (ordered)

   - Concrete task list with file/class targets (e.g., "CartController: remove DbContext injection; introduce ICartService; move merge workflow into CartService; introduce ICartRepository...")
## 3) Architecture rules enforced in this phase

   - E.g., "Controllers no longer reference DbContext" and "Services do not return ActionResult"
## 4) Acceptance criteria ("done" definition)

   - Objective checks (grep-level rules + code-level rules)
## 5) Test gate (from rubric)

   - Exact tests to add (minimum set) and which endpoints/use cases they cover
   - Test types (unit vs integration), frameworks, and suggested structure
   - What constitutes pass/fail and what to do on failure
## 6) Risks + mitigations

   - Behavior-change risk points and how tests will catch them

## C) Test strategy & tooling recommendations

- Preferred test frameworks (xUnit recommended)
- Integration testing approach for ASP.NET Core:
  - WebApplicationFactory/TestServer for API-level tests
  - SQL Server integration approach:
    - Prefer Testcontainers for SQL Server (or LocalDB where appropriate)
  - EF Core migrations application during tests
- Deterministic time (TimeProvider/ITimeProvider), seeded data strategy, test isolation and cleanup
- Avoiding flaky tests (idempotent setup, unique DB per test run, retries only where justified)

## D) Traceability matrix

- Map: audit finding IDs -> phase(s) that resolve them -> test(s) that verify resolution.
- Map: endpoint inventory -> phase migrated -> integration test coverage.

## E) "Definition of Layered Compliance" checklist

- A short checklist that can be rerun after each phase.

TESTING RUBRIC (GATE PER PHASE)
After completing each refactor phase, add the minimum tests required by the rubric below. Treat test pass as the gate to proceed.

PHASE 0 (baseline integration):
- Black-box integration tests for top critical endpoints (GET list, GET by id, POST, PUT/PATCH, DELETE, auth 401/403, validation 400).
- Verify status code + key response fields + DB state.

PHASE 1 (domain extraction):
- Domain unit tests: invariants, state transitions, calculations, edge cases, deterministic time.
- Baseline integration tests must still pass.

PHASE 2 (application/use cases):
- Application unit tests per use case: happy path + primary failure + security/ownership rule + side-effect correctness.
- Add at least 1 API->DB integration test per migrated endpoint.

PHASE 3 (infrastructure/EF extraction):
- Integration tests against real SQL Server behavior:
  migrations apply, constraints, query semantics, transactions, concurrency.

PHASE 4 (API contract hardening):
- Integration tests for authN/authZ, validation response shape, serialization contract, error mapping.

PHASE 5 (ship gate):
- Repeated runs: zero flakes.
- All critical workflows have domain+app unit coverage + at least one API+DB integration test.
- Every bug fixed gets a regression test.

IMPORTANT: PHASE NAMES
Use the rubric's phase numbering (0-5). If you need intermediate steps within a phase, name them "Phase 2a, 2b..." but they must still roll up to the rubric phase gate.

STARTING INSTRUCTIONS
## 1) Summarize the audit: key violations, most critical controllers, top workflows at risk (Cart merge, Checkout, etc.).

## 2) Propose which subsystems to migrate first (justify with audit severity and blast radius).

## 3) Then produce the full detailed plan in the required format.

If any information is missing from the audit (e.g., endpoint list incomplete), explicitly state assumptions and how you would validate them in Phase 0 tests.
Sobee API Layered Architecture Refactoring Plan
Audit Summary
Key Violations (from Audit Report)
| Finding ID | Severity | Description |
| --- | --- | --- |
| LAYER-001 | Blocker | All 14 domain controllers inject DbContext directly |
| LAYER-002 | Blocker | Business logic (checkout, cart merge, promo calc) embedded in controllers |
| LAYER-003 | Major | 60+ EF LINQ queries scattered across controllers |
| LAYER-004 | Major | Transaction management in controllers (Checkout, PayOrder) |
| LAYER-005 | Major | Stock validation duplicated in CartController & OrdersController |
| LAYER-006 | Major | Order status transition logic in controller |
| LAYER-007 | Minor | DTO projection logic scattered (no centralized mapping) |

Most Critical Controllers (by risk)
OrdersController - Checkout workflow (170 lines), transactions, stock decrement, promo snapshot
CartController - Cart merge logic (60+ lines), promo calculation, stock validation
ProductsController - Admin CRUD with stock management
AdminAnalyticsController - 15+ complex queries (read-only, lower risk)
Top Workflows at Risk
| Workflow | Controller | Risk Level | Why |
| --- | --- | --- | --- |
| Checkout | OrdersController | Critical | Transaction, stock, promo, order creation in single method |
| Cart Merge | CartController | Critical | Session->User cart transfer, item merging |
| Add to Cart | CartController | High | Stock validation, existing item detection |
| Apply Promo | CartController | High | Promo validation, usage history |
| Pay Order | OrdersController | High | Payment creation, status transition, transaction |
| Cancel Order | OrdersController | Medium | Status validation, stock restoration potential |

Subsystem Migration Order (Justified)
| Priority | Subsystem | Justification |
| --- | --- | --- |
| 1 | Cart | Foundation for checkout; merge logic is complex & fragile |
| 2 | Orders/Checkout | Highest transaction complexity; depends on Cart |
| 3 | Products | Admin CRUD; affects Cart/Orders via stock |
| 4 | Reviews | Independent; moderate complexity |
| 5 | Favorites | Simple; low risk |
| 6 | Admin Analytics | Read-only; can parallelize |
| 7 | Admin Promos/Users | Low frequency; lower priority |

## A) High-Level Roadmap

| Phase | Name | Goal | Subsystems | Why This Order |
| --- | --- | --- | --- | --- |
| 0 | Baseline Integration Tests | Lock current behavior before any changes | All critical endpoints | Must detect regressions |
| 1 | Domain Extraction | Extract business rules into testable domain logic | Cart (promo calc, stock validation), Orders (status transitions) | LAYER-002, LAYER-005, LAYER-006 are blockers |
| 2a | Cart Application Layer | Extract CartService + ICartRepository | Cart | Most complex merge logic; foundation for Orders |
| 2b | Orders Application Layer | Extract OrderService + IOrderRepository + InventoryService | Orders/Checkout | Depends on Cart patterns; highest transaction risk |
| 2c | Products Application Layer | Extract ProductService + IProductRepository | Products | Required for inventory consistency |
| 3 | Infrastructure Consolidation | Finalize repositories, optimize queries, add caching hooks | All migrated | LAYER-003 resolution |
| 4 | API Contract Hardening | Centralize error mapping, DTO mapping, validation responses | All | LAYER-007 resolution |
| 5 | Ship Gate | Full regression, flake detection, coverage validation | All | Production readiness |

## B) Phase-by-Phase Plan

## Phase 0: Baseline Integration Tests

Goal: Establish behavioral contracts before any refactoring. All subsequent phases must keep these tests green.

### 1) Scope

All critical endpoints from audit inventory:

| Subsystem | Endpoints to Cover |
| --- | --- |
| Cart | GET /api/cart, POST /api/cart/items, PUT /api/cart/items/{id}, DELETE /api/cart/items/{id}, DELETE /api/cart, POST /api/cart/promo/apply, DELETE /api/cart/promo |
| Orders | GET /api/orders/{id}, GET /api/orders/my, POST /api/orders/checkout, POST /api/orders/{id}/cancel, POST /api/orders/{id}/pay, PATCH /api/orders/{id}/status |
| Products | GET /api/products, GET /api/products/{id}, POST /api/products, PUT /api/products/{id}, DELETE /api/products/{id} |
| Auth | POST /api/auth/register, /identity/login (Identity endpoints) |

### 2) Tasks

| # | Task | Files |
| --- | --- | --- |
| 0.1 | Add Testcontainers.MsSql NuGet package | sobee_API.Tests/sobee_API.Tests.csproj |
| 0.2 | Create SobeeWebApplicationFactory with SQL Server container | sobee_API.Tests/Infrastructure/SobeeWebApplicationFactory.cs |
| 0.3 | Create test data seeder for products, users, promos | sobee_API.Tests/Infrastructure/TestDataSeeder.cs |
| 0.4 | Write Cart integration tests (7 endpoints) | sobee_API.Tests/Integration/CartEndpointTests.cs |
| 0.5 | Write Orders integration tests (6 endpoints) | sobee_API.Tests/Integration/OrdersEndpointTests.cs |
| 0.6 | Write Products integration tests (5+ endpoints) | sobee_API.Tests/Integration/ProductsEndpointTests.cs |
| 0.7 | Write Auth integration tests (register, login, 401/403) | sobee_API.Tests/Integration/AuthEndpointTests.cs |
| 0.8 | Document all response contracts (snapshot or assertion) | sobee_API.Tests/Contracts/ |

### 3) Architecture Rules Enforced

None yet (this phase is observation only).

### 4) Acceptance Criteria

 All critical endpoints have at least one happy-path test
 All critical endpoints have at least one error-path test (400, 401, 404, 409)
 Tests verify: status code, key response fields, database state changes
 Tests run against real SQL Server (via Testcontainers)
 All tests pass on CI
### 5) Test Gate

Minimum tests required:


CartEndpointTests:
  - GetCart_EmptyCart_Returns200WithEmptyItems
  - GetCart_WithItems_Returns200WithCorrectTotals
  - AddItem_ValidProduct_Returns200AndUpdatesCart
  - AddItem_InsufficientStock_Returns409
  - AddItem_InvalidProduct_Returns404
  - UpdateItem_ValidQuantity_Returns200
  - UpdateItem_ExceedsStock_Returns409
  - RemoveItem_ExistingItem_Returns200
  - ClearCart_Returns200AndEmptiesCart
  - ApplyPromo_ValidCode_Returns200WithDiscount
  - ApplyPromo_ExpiredCode_Returns400
  - ApplyPromo_AlreadyApplied_Returns409
  - RemovePromo_Returns200

OrdersEndpointTests:
  - Checkout_ValidCart_Returns201AndCreatesOrder
  - Checkout_EmptyCart_Returns400
  - Checkout_InsufficientStock_Returns409
  - Checkout_DecrementsStock
  - GetOrder_OwnOrder_Returns200
  - GetOrder_OtherUserOrder_Returns403Or404
  - GetMyOrders_Returns200WithPagination
  - CancelOrder_PendingOrder_Returns200
  - CancelOrder_ShippedOrder_Returns409
  - PayOrder_ValidPayment_Returns200
  - UpdateStatus_AdminOnly_Returns200Or403

ProductsEndpointTests:
  - GetProducts_Returns200WithPagination
  - GetProducts_WithSearch_FiltersCorrectly
  - GetProduct_Exists_Returns200
  - GetProduct_NotExists_Returns404
  - CreateProduct_Admin_Returns201
  - CreateProduct_NonAdmin_Returns403
  - UpdateProduct_Admin_Returns200
  - DeleteProduct_Admin_Returns204

AuthEndpointTests:
  - Register_ValidData_Returns201
  - Register_DuplicateEmail_Returns409
  - Login_ValidCredentials_ReturnsToken
  - Login_InvalidCredentials_Returns401
  - ProtectedEndpoint_NoToken_Returns401
  - AdminEndpoint_UserRole_Returns403
Pass criteria: 100% of baseline tests green.
Fail action: Do not proceed to Phase 1. Debug and fix test infrastructure.

### 6) Risks & Mitigations

| Risk | Mitigation |
| --- | --- |
| Existing behavior has bugs we'll lock in | Document known issues; exclude from baseline if intentional |
| Test data conflicts between test runs | Use unique IDs per test; cleanup after each test class |
| Testcontainers startup time | Use shared container per test class; parallelize test classes |

## 7) Documentation References

Integration tests in ASP.NET Core
WebApplicationFactory
Testcontainers for .NET
EF Core Testing
## Phase 1: Domain Extraction

Goal: Extract pure business rules into testable domain classes. No HTTP dependencies. No EF dependencies.

### 1) Scope

| Domain Concept | Current Location | Target |
| --- | --- | --- |
| Promo discount calculation | CartController:492-505, OrdersController | PromoCalculator static class |
| Stock validation | CartController:84-103, OrdersController:186-207 | StockValidator static class |
| Order status transitions | OrdersController:305-320, 418-468 | OrderStatusMachine class |
| Cart line total calculation | CartController:ProjectCartAsync | CartCalculator static class |

### 2) Tasks

| # | Task | Files |
| --- | --- | --- |
| 1.1 | Create Domain folder structure | sobee_API/Domain/ |
| 1.2 | Extract PromoCalculator.CalculateDiscount(subtotal, discountPercentage) | sobee_API/Domain/PromoCalculator.cs |
| 1.3 | Extract StockValidator.Validate(available, requested) -> StockValidationResult | sobee_API/Domain/StockValidator.cs |
| 1.4 | Extract OrderStatusMachine.CanTransition(from, to) and GetAllowedTransitions(from) | sobee_API/Domain/OrderStatusMachine.cs |
| 1.5 | Extract CartCalculator.CalculateSubtotal(items) and CalculateTotal(subtotal, discount) | sobee_API/Domain/CartCalculator.cs |
| 1.6 | Create ServiceResult<T> for error handling without exceptions | sobee_API/Domain/ServiceResult.cs |
| 1.7 | Update CartController to use domain classes (keep DbContext for now) | sobee_API/Controllers/CartController.cs |
| 1.8 | Update OrdersController to use domain classes (keep DbContext for now) | sobee_API/Controllers/OrdersController.cs |
| 1.9 | Write unit tests for all domain classes | sobee_API.Tests/Domain/*.cs |

### 3) Architecture Rules Enforced

 Domain classes have NO dependencies on EF Core (Microsoft.EntityFrameworkCore.*)
 Domain classes have NO dependencies on ASP.NET Core (Microsoft.AspNetCore.*)
 Domain classes are pure functions or simple state machines
 Domain classes use primitive types or simple DTOs as inputs/outputs
### 4) Acceptance Criteria

 grep -r "using Microsoft.EntityFrameworkCore" sobee_API/Domain/ returns nothing
 grep -r "using Microsoft.AspNetCore" sobee_API/Domain/ returns nothing
 All domain classes have >90% branch coverage in unit tests
 All Phase 0 integration tests still pass (behavior unchanged)
### 5) Test Gate

Domain unit tests required:


PromoCalculatorTests:
  - CalculateDiscount_ZeroPercent_ReturnsZero
  - CalculateDiscount_TenPercent_ReturnsCorrectAmount
  - CalculateDiscount_HundredPercent_ReturnsSubtotal
  - CalculateDiscount_NegativeSubtotal_ReturnsZero
  - CalculateDiscount_NegativePercent_ReturnsZero

StockValidatorTests:
  - Validate_SufficientStock_ReturnsValid
  - Validate_ExactStock_ReturnsValid
  - Validate_InsufficientStock_ReturnsInvalidWithAvailable
  - Validate_ZeroAvailable_ReturnsInvalid
  - Validate_ZeroRequested_ReturnsValid

OrderStatusMachineTests:
  - CanTransition_PendingToPaid_ReturnsTrue
  - CanTransition_PendingToShipped_ReturnsFalse
  - CanTransition_PaidToShipped_ReturnsTrue
  - CanTransition_ShippedToDelivered_ReturnsTrue
  - CanTransition_DeliveredToCancelled_ReturnsFalse
  - CanTransition_PendingToCancelled_ReturnsTrue
  - GetAllowedTransitions_Pending_ReturnsCorrectSet
  - IsCancellable_Pending_ReturnsTrue
  - IsCancellable_Shipped_ReturnsFalse

CartCalculatorTests:
  - CalculateSubtotal_EmptyItems_ReturnsZero
  - CalculateSubtotal_SingleItem_ReturnsCorrectTotal
  - CalculateSubtotal_MultipleItems_SumsCorrectly
  - CalculateTotal_NoDiscount_ReturnsSubtotal
  - CalculateTotal_WithDiscount_SubtractsDiscount
  - CalculateTotal_DiscountExceedsSubtotal_ReturnsZero
Pass criteria: All domain unit tests pass + all Phase 0 integration tests pass.
Fail action: Fix domain logic or controller integration before proceeding.

### 6) Risks & Mitigations

| Risk | Mitigation |
| --- | --- |
| Calculation logic differs from original | Compare domain class output with hardcoded expected values from current behavior |
| Status transitions have edge cases | Map all transitions from OrderStatuses.cs constants; test exhaustively |
| Changing controller code breaks behavior | Run Phase 0 tests after each controller modification |

## Phase 2a: Cart Application Layer

Goal: Extract CartService and ICartRepository. Controllers become thin HTTP handlers.

### 1) Scope

| Endpoint | Current Handler | Target Service Method |
| --- | --- | --- |
| GET /api/cart | CartController.GetCart | ICartService.GetCartAsync |
| POST /api/cart/items | CartController.AddItem | ICartService.AddItemAsync |
| PUT /api/cart/items/{id} | CartController.UpdateItem | ICartService.UpdateItemAsync |
| DELETE /api/cart/items/{id} | CartController.RemoveItem | ICartService.RemoveItemAsync |
| DELETE /api/cart | CartController.ClearCart | ICartService.ClearCartAsync |
| POST /api/cart/promo/apply | CartController.ApplyPromo | ICartService.ApplyPromoAsync |
| DELETE /api/cart/promo | CartController.RemovePromo | ICartService.RemovePromoAsync |

### 2) Tasks

| # | Task | Files |
| --- | --- | --- |
| 2a.1 | Create service interfaces folder | sobee_API/Services/Interfaces/ |
| 2a.2 | Define ICartService interface | sobee_API/Services/Interfaces/ICartService.cs |
| 2a.3 | Define ICartRepository interface | Sobee.Domain/Repositories/ICartRepository.cs |
| 2a.4 | Define IPromoRepository interface | Sobee.Domain/Repositories/IPromoRepository.cs |
| 2a.5 | Define IProductRepository interface (for stock checks) | Sobee.Domain/Repositories/IProductRepository.cs |
| 2a.6 | Implement CartRepository | Sobee.Domain/Repositories/CartRepository.cs |
| 2a.7 | Implement PromoRepository | Sobee.Domain/Repositories/PromoRepository.cs |
| 2a.8 | Implement ProductRepository (partial, for stock) | Sobee.Domain/Repositories/ProductRepository.cs |
| 2a.9 | Implement CartService with full cart logic | sobee_API/Services/CartService.cs |
| 2a.10 | Register services in DI | sobee_API/Program.cs |
| 2a.11 | Refactor CartController to use ICartService | sobee_API/Controllers/CartController.cs |
| 2a.12 | Remove _db field from CartController | sobee_API/Controllers/CartController.cs |
| 2a.13 | Write unit tests for CartService | sobee_API.Tests/Services/CartServiceTests.cs |
| 2a.14 | Write repository integration tests | sobee_API.Tests/Repositories/CartRepositoryTests.cs |

### 3) Architecture Rules Enforced

 CartController does not reference SobeecoredbContext
 CartController does not contain await _db. calls
 CartService does not reference ControllerBase, IActionResult, HttpContext
 CartService returns ServiceResult<CartDto> not IActionResult
 CartRepository does not contain business logic (only query shaping)
### 4) Acceptance Criteria

 grep "SobeecoredbContext" sobee_API/Controllers/CartController.cs returns nothing
 grep "IActionResult" sobee_API/Services/CartService.cs returns nothing
 All 7 cart endpoints work identically (Phase 0 tests pass)
 CartService has unit tests with mocked repositories
 CartRepository has integration tests against SQL Server
### 5) Test Gate

Application unit tests (CartService):


CartServiceTests:
  - GetCartAsync_NoCart_ReturnsEmptyCart
  - GetCartAsync_ExistingCart_ReturnsWithItems
  - GetCartAsync_AuthenticatedUser_MergesGuestCart
  - AddItemAsync_NewItem_CreatesCartItem
  - AddItemAsync_ExistingItem_IncrementsQuantity
  - AddItemAsync_InsufficientStock_ReturnsError
  - AddItemAsync_InvalidProduct_ReturnsError
  - UpdateItemAsync_ValidQuantity_UpdatesItem
  - UpdateItemAsync_ZeroQuantity_RemovesItem
  - UpdateItemAsync_ExceedsStock_ReturnsError
  - UpdateItemAsync_ItemNotFound_ReturnsError
  - RemoveItemAsync_ExistingItem_RemovesItem
  - RemoveItemAsync_ItemNotFound_ReturnsError
  - ClearCartAsync_ClearsAllItems
  - ApplyPromoAsync_ValidCode_AppliesDiscount
  - ApplyPromoAsync_ExpiredCode_ReturnsError
  - ApplyPromoAsync_AlreadyUsed_ReturnsError
  - RemovePromoAsync_RemovesAppliedPromo
Repository integration tests:


CartRepositoryTests:
  - FindByUserIdAsync_Exists_ReturnsCart
  - FindByUserIdAsync_NotExists_ReturnsNull
  - FindBySessionIdAsync_Exists_ReturnsCart
  - CreateAsync_CreatesCart
  - AddCartItemAsync_AddsItem
  - UpdateCartItemAsync_UpdatesQuantity
  - RemoveCartItemAsync_RemovesItem
  - ClearCartItemsAsync_RemovesAllItems
Pass criteria: All unit tests + repository tests + Phase 0 integration tests pass.
Fail action: Debug service/repository interaction; verify DI registration.

### 6) Risks & Mitigations

| Risk | Mitigation |
| --- | --- |
| Cart merge logic has subtle edge cases | Dedicated tests for each merge scenario (guest->user, user has cart, user has no cart) |
| Transaction boundaries differ | CartService uses single SaveChanges; verify atomicity in tests |
| GuestSessionService integration | Keep GuestSessionService as infrastructure service; inject into CartService |

## Phase 2b: Orders Application Layer

Goal: Extract OrderService, IOrderRepository, and InventoryService. Handle complex checkout workflow.

### 1) Scope

| Endpoint | Current Handler | Target Service Method |
| --- | --- | --- |
| GET /api/orders/{id} | OrdersController.GetOrder | IOrderService.GetOrderAsync |
| GET /api/orders/my | OrdersController.GetMyOrders | IOrderService.GetUserOrdersAsync |
| POST /api/orders/checkout | OrdersController.Checkout | IOrderService.CheckoutAsync |
| POST /api/orders/{id}/cancel | OrdersController.CancelOrder | IOrderService.CancelOrderAsync |
| POST /api/orders/{id}/pay | OrdersController.PayOrder | IOrderService.PayOrderAsync |
| PATCH /api/orders/{id}/status | OrdersController.UpdateOrderStatus | IOrderService.UpdateStatusAsync |

### 2) Tasks

| # | Task | Files |
| --- | --- | --- |
| 2b.1 | Define IOrderService interface | sobee_API/Services/Interfaces/IOrderService.cs |
| 2b.2 | Define IOrderRepository interface | Sobee.Domain/Repositories/IOrderRepository.cs |
| 2b.3 | Define IInventoryService interface | sobee_API/Services/Interfaces/IInventoryService.cs |
| 2b.4 | Define IPaymentRepository interface | Sobee.Domain/Repositories/IPaymentRepository.cs |
| 2b.5 | Implement OrderRepository | Sobee.Domain/Repositories/OrderRepository.cs |
| 2b.6 | Implement PaymentRepository | Sobee.Domain/Repositories/PaymentRepository.cs |
| 2b.7 | Implement InventoryService (stock validation + decrement) | sobee_API/Services/InventoryService.cs |
| 2b.8 | Implement OrderService with checkout workflow | sobee_API/Services/OrderService.cs |
| 2b.9 | Move transaction management to OrderService.CheckoutAsync | sobee_API/Services/OrderService.cs |
| 2b.10 | Register services in DI | sobee_API/Program.cs |
| 2b.11 | Refactor OrdersController to use IOrderService | sobee_API/Controllers/OrdersController.cs |
| 2b.12 | Remove _db field from OrdersController | sobee_API/Controllers/OrdersController.cs |
| 2b.13 | Write unit tests for OrderService | sobee_API.Tests/Services/OrderServiceTests.cs |
| 2b.14 | Write unit tests for InventoryService | sobee_API.Tests/Services/InventoryServiceTests.cs |
| 2b.15 | Write repository integration tests | sobee_API.Tests/Repositories/OrderRepositoryTests.cs |

### 3) Architecture Rules Enforced

 OrdersController does not reference SobeecoredbContext
 OrdersController does not contain transaction code (BeginTransactionAsync)
 OrderService manages transactions via IDbContextTransaction or Unit of Work
 OrderService uses ICartService (not ICartRepository directly) for cart operations
 InventoryService is the single source of truth for stock operations
### 4) Acceptance Criteria

 grep "SobeecoredbContext" sobee_API/Controllers/OrdersController.cs returns nothing
 grep "BeginTransactionAsync" sobee_API/Controllers/OrdersController.cs returns nothing
 Checkout creates order + decrements stock + clears cart atomically
 All 6 orders endpoints work identically (Phase 0 tests pass)
 OrderService has unit tests with mocked dependencies
### 5) Test Gate

Application unit tests (OrderService):


OrderServiceTests:
  - GetOrderAsync_OwnOrder_ReturnsOrder
  - GetOrderAsync_OtherUserOrder_ReturnsUnauthorized
  - GetOrderAsync_NotFound_ReturnsNotFound
  - GetUserOrdersAsync_ReturnsPaginatedOrders
  - CheckoutAsync_ValidCart_CreatesOrder
  - CheckoutAsync_EmptyCart_ReturnsError
  - CheckoutAsync_InsufficientStock_ReturnsErrorAndNoStockChange
  - CheckoutAsync_AppliesPromoDiscount
  - CheckoutAsync_ClearsCartAfterSuccess
  - CheckoutAsync_RollsBackOnFailure
  - CancelOrderAsync_PendingOrder_CancelsOrder
  - CancelOrderAsync_ShippedOrder_ReturnsError
  - CancelOrderAsync_OtherUserOrder_ReturnsUnauthorized
  - PayOrderAsync_ValidPayment_UpdatesStatus
  - PayOrderAsync_AlreadyPaid_ReturnsError
  - UpdateStatusAsync_ValidTransition_UpdatesStatus
  - UpdateStatusAsync_InvalidTransition_ReturnsError
  - UpdateStatusAsync_SetsTimestamps (ShippedDate, DeliveredDate)

InventoryServiceTests:
  - ValidateStockAsync_SufficientStock_ReturnsValid
  - ValidateStockAsync_InsufficientStock_ReturnsInvalid
  - DecrementStockAsync_DecrementsCorrectAmount
  - DecrementStockAsync_ConcurrentRequests_HandlesCorrectly
  - ReserveStockAsync_TemporarilyHoldsStock (if implementing)
Repository integration tests:


OrderRepositoryTests:
  - GetByIdAsync_Exists_ReturnsOrder
  - GetByIdWithItemsAsync_IncludesItems
  - GetByUserIdAsync_ReturnsPaginated
  - CreateAsync_CreatesOrderWithItems
  - UpdateAsync_UpdatesStatus
Pass criteria: All unit tests + repository tests + Phase 0 integration tests pass.
Fail action: Verify transaction boundaries; check stock decrement atomicity.

### 6) Risks & Mitigations

| Risk | Mitigation |
| --- | --- |
| Transaction rollback doesn't restore stock | Test explicitly that failed checkout leaves stock unchanged |
| Concurrent checkouts cause overselling | Add integration test with parallel checkout attempts |
| Promo snapshot logic differs | Verify promo fields on created order match original behavior |

## Phase 2c: Products Application Layer

Goal: Extract ProductService. Complete IProductRepository implementation.

### 1) Scope

| Endpoint | Target Service Method |
| --- | --- |
| GET /api/products | IProductService.GetProductsAsync |
| GET /api/products/{id} | IProductService.GetProductAsync |
| POST /api/products | IProductService.CreateProductAsync |
| PUT /api/products/{id} | IProductService.UpdateProductAsync |
| DELETE /api/products/{id} | IProductService.DeleteProductAsync |
| POST /api/products/{id}/images | IProductService.AddProductImageAsync |
| DELETE /api/products/{id}/images/{imgId} | IProductService.DeleteProductImageAsync |

### 2) Tasks

| # | Task | Files |
| --- | --- | --- |
| 2c.1 | Define IProductService interface | sobee_API/Services/Interfaces/IProductService.cs |
| 2c.2 | Complete IProductRepository with search/filter | Sobee.Domain/Repositories/IProductRepository.cs |
| 2c.3 | Implement ProductRepository (full) | Sobee.Domain/Repositories/ProductRepository.cs |
| 2c.4 | Implement ProductService | sobee_API/Services/ProductService.cs |
| 2c.5 | Register services in DI | sobee_API/Program.cs |
| 2c.6 | Refactor ProductsController | sobee_API/Controllers/ProductsController.cs |
| 2c.7 | Write unit tests | sobee_API.Tests/Services/ProductServiceTests.cs |
| 2c.8 | Write repository integration tests | sobee_API.Tests/Repositories/ProductRepositoryTests.cs |

### 3) Architecture Rules Enforced

 ProductsController does not reference SobeecoredbContext
 ProductService handles admin authorization checks
 ProductRepository handles complex search/filter/sort queries
### 4) Acceptance Criteria

 All 7 product endpoints work identically
 Search, filter, sort, pagination work correctly
 Admin-only endpoints reject non-admin users
### 5) Test Gate

ProductServiceTests:
  - GetProductsAsync_ReturnsPagedResults
  - GetProductsAsync_WithSearch_FiltersCorrectly
  - GetProductsAsync_WithCategory_FiltersCorrectly
  - GetProductsAsync_WithSort_SortsCorrectly
  - GetProductAsync_Exists_ReturnsProduct
  - GetProductAsync_NotExists_ReturnsNotFound
  - GetProductAsync_IncludesImages
  - CreateProductAsync_ValidData_CreatesProduct
  - UpdateProductAsync_ValidData_UpdatesProduct
  - UpdateProductAsync_NotFound_ReturnsError
  - DeleteProductAsync_Exists_DeletesProduct
  - AddProductImageAsync_AddsImage
  - DeleteProductImageAsync_DeletesImage
Pass criteria: All tests pass.

## Phase 3: Infrastructure Consolidation

Goal: Finalize repository layer. Optimize queries. Add query specifications if needed.

### 1) Scope

All remaining controllers (Reviews, Favorites, Admin*)
Query optimization across all repositories
Caching infrastructure (if needed)
### 2) Tasks

| # | Task | Files |
| --- | --- | --- |
| 3.1 | Extract ReviewService + IReviewRepository | sobee_API/Services/ReviewService.cs, Sobee.Domain/Repositories/ReviewRepository.cs |
| 3.2 | Extract FavoriteService + IFavoriteRepository | sobee_API/Services/FavoriteService.cs, Sobee.Domain/Repositories/FavoriteRepository.cs |
| 3.3 | Extract AdminPromoService | sobee_API/Services/AdminPromoService.cs |
| 3.4 | Extract AdminUserService | sobee_API/Services/AdminUserService.cs |
| 3.5 | Extract AnalyticsService + IAnalyticsRepository | sobee_API/Services/AnalyticsService.cs |
| 3.6 | Refactor all remaining controllers | sobee_API/Controllers/*.cs |
| 3.7 | Audit all repositories for N+1 queries | All repositories |
| 3.8 | Add .AsNoTracking() to read-only queries | All repositories |
| 3.9 | Add query result caching (if applicable) | Repository decorators or service layer |
| 3.10 | Write integration tests for all repositories | sobee_API.Tests/Repositories/*.cs |

### 3) Architecture Rules Enforced

 No controller references SobeecoredbContext (zero violations)
 All EF queries are in repositories
 Read-only queries use AsNoTracking()
 Complex queries use projection to DTOs (not loading full entities)
### 4) Acceptance Criteria

 grep -r "SobeecoredbContext" sobee_API/Controllers/ returns nothing
 grep -r "ApplicationDbContext" sobee_API/Controllers/ returns nothing (except AdminUsersController if using UserManager)
 No N+1 queries detected in common operations
 All Phase 0 integration tests pass
### 5) Test Gate

Integration tests for real SQL Server behavior:


RepositoryIntegrationTests:
  - AllRepositories_MigrationsApply
  - CartRepository_ConcurrentAccess_HandlesCorrectly
  - OrderRepository_TransactionRollback_WorksCorrectly
  - ProductRepository_ComplexSearch_PerformsCorrectly
  - AnalyticsRepository_AggregateQueries_ReturnCorrectResults

ConstraintTests:
  - CartItem_ForeignKeyToProduct_EnforcedByDb
  - OrderItem_ForeignKeyToOrder_EnforcedByDb
  - Review_UniquePerUserProduct_EnforcedByDb (if applicable)
Pass criteria: All integration tests pass with real SQL Server.

### 6) Risks & Mitigations

| Risk | Mitigation |
| --- | --- |
| Analytics queries are complex and slow | Profile with SQL Server profiler; add indexes if needed |
| AdminUsersController uses UserManager directly | This is acceptable; UserManager is the Identity abstraction |

## Phase 4: API Contract Hardening

Goal: Standardize error responses, DTO mapping, validation responses, and serialization contracts.

### 1) Scope

Error response standardization
DTO mapping centralization (AutoMapper or manual)
Validation response format
API versioning consideration
### 2) Tasks

| # | Task | Files |
| --- | --- | --- |
| 4.1 | Add AutoMapper (optional) or create mapping extension methods | sobee_API/Mapping/*.cs |
| 4.2 | Centralize error mapping in ApiControllerBase or middleware | sobee_API/Controllers/ApiControllerBase.cs |
| 4.3 | Standardize ServiceResult<T> to HTTP response mapping | sobee_API/Extensions/ServiceResultExtensions.cs |
| 4.4 | Add API response contracts (OpenAPI schemas) | sobee_API/Contracts/*.cs |
| 4.5 | Add contract tests for response shapes | sobee_API.Tests/Contracts/*.cs |
| 4.6 | Verify all error codes are documented | sobee_API/DTOs/Common/ErrorCodes.cs |

### 3) Architecture Rules Enforced

 All error responses use ApiErrorResponse format
 All validation errors return consistent shape
 All DTOs have explicit mapping (no implicit entity exposure)
### 4) Acceptance Criteria

 All 4xx/5xx responses match ApiErrorResponse schema
 Validation errors include field-level details
 No entity types exposed in API responses
 OpenAPI spec accurately reflects actual responses
### 5) Test Gate

ContractTests:
  - ValidationError_ReturnsConsistentShape
  - NotFoundError_ReturnsConsistentShape
  - UnauthorizedError_ReturnsConsistentShape
  - ConflictError_ReturnsConsistentShape
  - ServerError_ReturnsConsistentShape
  - SuccessResponse_MatchesOpenApiSchema (per endpoint)

SerializationTests:
  - CartResponse_SerializesToExpectedJson
  - OrderResponse_SerializesToExpectedJson
  - ProductListResponse_SerializesToExpectedJson
  - DateTimes_SerializeAsIso8601
  - Decimals_SerializeWithCorrectPrecision
Pass criteria: All contract tests pass.

## Phase 5: Ship Gate

Goal: Validate production readiness. Zero flakes. Full coverage. Regression tests for any bugs found.

### 1) Scope

Full test suite execution
Flake detection
Coverage analysis
Performance baseline
### 2) Tasks

| # | Task | Files |
| --- | --- | --- |
| 5.1 | Run full test suite 10x consecutively | CI pipeline |
| 5.2 | Identify and fix any flaky tests | Various |
| 5.3 | Generate code coverage report | CI pipeline |
| 5.4 | Verify critical paths have unit + integration coverage | Coverage report |
| 5.5 | Add performance baseline tests (optional) | sobee_API.Tests/Performance/*.cs |
| 5.6 | Document architecture in ARCHITECTURE.md | sobee_API/ARCHITECTURE.md |
| 5.7 | Update CLAUDE.md with layer rules | CLAUDE.md |

## 3) Acceptance Criteria

 10 consecutive test runs with 0 failures
 >80% line coverage on Services
 >70% line coverage on Repositories
 All critical workflows (Checkout, Cart Merge, Pay Order) have both unit and integration tests
 Every bug fixed during refactor has a regression test
## 4) Test Gate

Ship criteria:

 All Phase 0-4 tests pass
 Zero flaky tests in 10 runs
 Coverage thresholds met
 No known regressions
## C) Test Strategy & Tooling Recommendations

### Frameworks

| Purpose | Recommendation |
| --- | --- |
| Unit testing | xUnit (industry standard, good async support) |
| Mocking | NSubstitute or Moq (NSubstitute preferred for cleaner syntax) |
| Assertions | FluentAssertions (readable, chainable) |
| Integration testing | Microsoft.AspNetCore.Mvc.Testing (WebApplicationFactory) |
| Database | Testcontainers.MsSql (real SQL Server in Docker) |
| Coverage | Coverlet + ReportGenerator |

### Project Structure
```text

sobee_API.Tests/
|-- Infrastructure/
|   |-- SobeeWebApplicationFactory.cs      # WebApplicationFactory with Testcontainers
|   |-- TestDataSeeder.cs                  # Seed products, users, promos
|   |-- TestAuthHandler.cs                 # Fake auth for testing
|   \-- DatabaseFixture.cs                 # Shared container fixture
|-- Domain/
|   |-- PromoCalculatorTests.cs
|   |-- StockValidatorTests.cs
|   |-- OrderStatusMachineTests.cs
|   \-- CartCalculatorTests.cs
|-- Services/
|   |-- CartServiceTests.cs
|   |-- OrderServiceTests.cs
|   |-- InventoryServiceTests.cs
|   \-- ProductServiceTests.cs
|-- Repositories/
|   |-- CartRepositoryTests.cs
|   |-- OrderRepositoryTests.cs
|   \-- ProductRepositoryTests.cs
|-- Integration/
|   |-- CartEndpointTests.cs
|   |-- OrdersEndpointTests.cs
|   |-- ProductsEndpointTests.cs
|   \-- AuthEndpointTests.cs
\-- Contracts/
    |-- ErrorResponseContractTests.cs
    \-- SerializationContractTests.cs
```
### WebApplicationFactory Configuration
```csharp

public class SobeeWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _sqlContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Replace connection strings with container
            var connectionString = _sqlContainer.GetConnectionString();
            
            services.RemoveAll<DbContextOptions<SobeecoredbContext>>();
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            
            services.AddDbContext<SobeecoredbContext>(options =>
                options.UseSqlServer(connectionString));
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));
            
            // Replace TimeProvider for deterministic time
            services.AddSingleton(TimeProvider.System); // or FakeTimeProvider
        });
    }

    public async Task InitializeAsync()
    {
        await _sqlContainer.StartAsync();
        
        // Apply migrations
        using var scope = Services.CreateScope();
        var coreDb = scope.ServiceProvider.GetRequiredService<SobeecoredbContext>();
        var identityDb = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await coreDb.Database.MigrateAsync();
        await identityDb.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await _sqlContainer.DisposeAsync();
    }
}
```
### Test Isolation Strategy

| Concern | Strategy |
| --- | --- |
| Database state | Each test class gets fresh seeded data; use transactions that rollback |
| User context | Use test auth handler with configurable claims |
| Time | Inject TimeProvider; use FakeTimeProvider in tests |
| External services | Not applicable (no external dependencies noted) |
| Parallel execution | Separate database per test class (or use transactions) |

### Avoiding Flaky Tests

No shared mutable state between tests
Unique identifiers per test (GUIDs for emails, product names)
Explicit waits only where truly async (not arbitrary delays)
Deterministic time via TimeProvider
Fresh database per test class
No dependency on test execution order
### Documentation References

ASP.NET Core Integration Testing
EF Core Testing with Real Database
Testcontainers .NET
xUnit Documentation
FluentAssertions
TimeProvider in .NET 8
## D) Traceability Matrix

Audit Finding -> Phase -> Tests
| Finding ID | Description | Resolved In | Verification Tests |
| --- | --- | --- | --- |
| LAYER-001 | Controllers inject DbContext | Phase 2a, 2b, 2c, 3 | Grep check + all integration tests |
| LAYER-002 | Business logic in controllers | Phase 1, 2a, 2b | Domain unit tests + service unit tests |
| LAYER-003 | EF queries in controllers | Phase 2a, 2b, 2c, 3 | Repository integration tests |
| LAYER-004 | Transactions in controllers | Phase 2b | OrderServiceTests.CheckoutAsync_RollsBackOnFailure |
| LAYER-005 | Stock validation duplicated | Phase 1, 2b | StockValidatorTests, InventoryServiceTests |
| LAYER-006 | Status transition in controller | Phase 1 | OrderStatusMachineTests |
| LAYER-007 | DTO mapping scattered | Phase 4 | ContractTests |

Endpoint -> Phase -> Test Coverage
| Endpoint | Phase Migrated | Unit Test | Integration Test |
| --- | --- | --- | --- |
| GET /api/cart | 2a | CartServiceTests.GetCartAsync_* | CartEndpointTests.GetCart_* |
| POST /api/cart/items | 2a | CartServiceTests.AddItemAsync_* | CartEndpointTests.AddItem_* |
| PUT /api/cart/items/{id} | 2a | CartServiceTests.UpdateItemAsync_* | CartEndpointTests.UpdateItem_* |
| DELETE /api/cart/items/{id} | 2a | CartServiceTests.RemoveItemAsync_* | CartEndpointTests.RemoveItem_* |
| DELETE /api/cart | 2a | CartServiceTests.ClearCartAsync_* | CartEndpointTests.ClearCart_* |
| POST /api/cart/promo/apply | 2a | CartServiceTests.ApplyPromoAsync_* | CartEndpointTests.ApplyPromo_* |
| DELETE /api/cart/promo | 2a | CartServiceTests.RemovePromoAsync_* | CartEndpointTests.RemovePromo_* |
| GET /api/orders/{id} | 2b | OrderServiceTests.GetOrderAsync_* | OrdersEndpointTests.GetOrder_* |
| GET /api/orders/my | 2b | OrderServiceTests.GetUserOrdersAsync_* | OrdersEndpointTests.GetMyOrders_* |
| POST /api/orders/checkout | 2b | OrderServiceTests.CheckoutAsync_* | OrdersEndpointTests.Checkout_* |
| POST /api/orders/{id}/cancel | 2b | OrderServiceTests.CancelOrderAsync_* | OrdersEndpointTests.CancelOrder_* |
| POST /api/orders/{id}/pay | 2b | OrderServiceTests.PayOrderAsync_* | OrdersEndpointTests.PayOrder_* |
| PATCH /api/orders/{id}/status | 2b | OrderServiceTests.UpdateStatusAsync_* | OrdersEndpointTests.UpdateStatus_* |
| GET /api/products | 2c | ProductServiceTests.GetProductsAsync_* | ProductsEndpointTests.GetProducts_* |
| GET /api/products/{id} | 2c | ProductServiceTests.GetProductAsync_* | ProductsEndpointTests.GetProduct_* |
| POST /api/products | 2c | ProductServiceTests.CreateProductAsync_* | ProductsEndpointTests.CreateProduct_* |
| PUT /api/products/{id} | 2c | ProductServiceTests.UpdateProductAsync_* | ProductsEndpointTests.UpdateProduct_* |
| DELETE /api/products/{id} | 2c | ProductServiceTests.DeleteProductAsync_* | ProductsEndpointTests.DeleteProduct_* |
| POST /api/products/{id}/images | 2c | ProductServiceTests.AddProductImageAsync_* | ProductsEndpointTests.AddImage_* |
| DELETE /api/products/{id}/images/{imgId} | 2c | ProductServiceTests.DeleteProductImageAsync_* | ProductsEndpointTests.DeleteImage_* |
| GET /api/reviews/* | 3 | ReviewServiceTests.* | ReviewsEndpointTests.* |
| GET /api/favorites | 3 | FavoriteServiceTests.* | FavoritesEndpointTests.* |
| GET /api/admin/analytics/* | 3 | AnalyticsServiceTests.* | AdminAnalyticsEndpointTests.* |
| GET /api/admin/promos | 3 | AdminPromoServiceTests.* | AdminPromosEndpointTests.* |
| GET /api/admin/users | 3 | AdminUserServiceTests.* | AdminUsersEndpointTests.* |

## E) Definition of Layered Compliance Checklist

Run this checklist after each phase to verify compliance:

### Controller Layer Compliance

 No controller file contains SobeecoredbContext or ApplicationDbContext (except through Identity's UserManager)
 No controller file contains await _db. or _context.
 No controller file contains BeginTransactionAsync or SaveChangesAsync
 No controller file contains EF-specific types (DbSet, IQueryable<TEntity>)
 All controller methods are <=30 lines (excluding attributes and braces)
 Controllers only use: routing, model binding, validation, auth, and response mapping
**Verification command:**


```bash
grep -rn "SobeecoredbContext\|ApplicationDbContext\|\.SaveChanges\|BeginTransaction\|DbSet<" sobee_API/Controllers/
# Expected: No results (or only UserManager-related in Auth controllers)
```

### Service Layer Compliance

 No service file contains ControllerBase, IActionResult, ActionResult, HttpContext
 No service file contains [Http*] attributes
 All services return ServiceResult<T> or domain DTOs
 Services inject repositories via interfaces (not DbContext directly)
 Services contain business logic (validation, calculations, workflow orchestration)
**Verification command:**


```bash
grep -rn "IActionResult\|ActionResult\|HttpContext\|ControllerBase" sobee_API/Services/
# Expected: No results
```

### Repository Layer Compliance

 Repositories only contain data access logic (no business rules)
 Repositories use AsNoTracking() for read-only queries
 Repositories return entities or projections (not DTOs with business logic)
 No pricing, discount, or status transition logic in repositories
**Verification command:**


```bash
grep -rn "CalculateDiscount\|CanTransition\|ValidateStock" Sobee.Domain/Repositories/
# Expected: No results
```

### Domain Layer Compliance

 Domain classes have no EF Core references
 Domain classes have no ASP.NET Core references
 Domain classes are pure functions or simple state machines
 Domain classes have >90% unit test coverage
**Verification command:**


```bash
grep -rn "Microsoft.EntityFrameworkCore\|Microsoft.AspNetCore" sobee_API/Domain/
# Expected: No results
```

### Test Compliance

 All Phase 0 baseline tests pass
 All domain unit tests pass
 All service unit tests pass
 All repository integration tests pass
 All endpoint integration tests pass
 Zero flaky tests in 5 consecutive runs
### Summary

This plan provides a structured, test-gated approach to refactoring the Sobee API from a monolithic controller-centric architecture to a proper layered architecture. The key principles are:

Lock behavior first (Phase 0) before any changes
Extract domain logic (Phase 1) into testable pure functions
Migrate vertically (Phase 2a-c) by subsystem to minimize blast radius
Consolidate infrastructure (Phase 3) after patterns are established
Harden contracts (Phase 4) for API stability
Validate thoroughly (Phase 5) before shipping
Each phase has explicit acceptance criteria and test gates that must pass before proceeding. This ensures behavior stability throughout the refactor and builds confidence in the final architecture.
