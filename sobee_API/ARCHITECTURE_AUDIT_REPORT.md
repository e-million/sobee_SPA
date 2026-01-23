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

---

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

---

### LAYER-003: EF Queries Scattered in Controllers (Major)

**Severity:** Major
**Rule Violated:** Data access must be isolated in repositories
**Files Affected:** All domain controllers

**Examples:**

**CartController** - 9+ distinct EF queries:
```csharp
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
```

**AdminAnalyticsController** - 15+ complex analytical queries spanning 600+ lines

**Why It's a Problem:**
- Query optimization is scattered and hard to audit
- Duplicate query patterns across controllers
- Cannot easily add caching layer
- Difficult to profile database performance

---

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

---

### LAYER-005: Stock Validation Logic in Controller (Major)

**Severity:** Major
**Rule Violated:** Inventory rules are business logic, not presentation logic
**Files Affected:** CartController, OrdersController

**CartController** (Lines 84-103):
```csharp
// Stock check for new item
if (request.Quantity > product.IntStockAmount)
    return StockConflict(product.IntProductId, product.IntStockAmount, request.Quantity);

// Stock check for increment
if (newQuantity > product.IntStockAmount)
    return StockConflict(product.IntProductId, product.IntStockAmount, newQuantity);
```

**OrdersController** (Lines 186-207):
```csharp
foreach (var cartItem in cart.TcartItems)
{
    if (product.IntStockAmount < qty)
    {
        return ConflictError("Insufficient stock.", ...);
    }
    product.IntStockAmount -= qty;  // Inventory modification
}
```

**Why It's a Problem:**
- Stock rules duplicated across Cart and Orders
- Cannot enforce stock policies uniformly
- Race conditions possible without proper service-level locking

---

### LAYER-006: Order Status Transition Logic in Controller (Major)

**Severity:** Major
**Rule Violated:** Status workflow rules belong in service/domain layer
**File:** OrdersController.cs:305-320, 418-468

```csharp
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
```

**Why It's a Problem:**
- State machine logic in controller
- Workflow rules tied to HTTP layer
- Cannot trigger status changes from background jobs easily

---

### LAYER-007: DTO Projection in Controllers (Minor)

**Severity:** Minor
**Rule Violated:** DTO mapping should be centralized
**Files:** All controllers

**Example** (CartController Lines 473-527):
```csharp
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
```

**Why It's a Problem:**
- Mapping logic scattered across controllers
- No centralized mapping configuration (like AutoMapper profiles)
- Hard to maintain consistency across endpoints

---

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
```csharp
// File: sobee_API/Services/ICartService.cs
public interface ICartService
{
    Task<CartDto> GetCartAsync(string? userId, string? sessionId);
    Task<CartDto> AddItemAsync(string? userId, string? sessionId, int productId, int quantity);
    Task<CartDto> UpdateItemAsync(string? userId, string? sessionId, int cartItemId, int quantity);
    Task<CartDto> RemoveItemAsync(string? userId, string? sessionId, int cartItemId);
    Task<CartDto> ClearCartAsync(string? userId, string? sessionId);
    Task<PromoResult> ApplyPromoAsync(string? userId, string? sessionId, string promoCode);
    Task<CartDto> RemovePromoAsync(string? userId, string? sessionId);
}

// File: sobee_API/Services/IOrderService.cs
public interface IOrderService
{
    Task<OrderDto> GetOrderAsync(string? userId, string? sessionId, int orderId);
    Task<PagedResult<OrderDto>> GetUserOrdersAsync(string userId, int page, int pageSize);
    Task<OrderDto> CheckoutAsync(string? userId, string? sessionId, CheckoutRequest request);
    Task<OrderDto> CancelOrderAsync(string? userId, string? sessionId, int orderId);
    Task<OrderDto> PayOrderAsync(string? userId, string? sessionId, int orderId, PayOrderRequest request);
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
```

**Repositories (Data Layer):**
```csharp
// File: Sobee.Domain/Repositories/ICartRepository.cs
public interface ICartRepository
{
    Task<TshoppingCart?> FindByUserIdAsync(string userId);
    Task<TshoppingCart?> FindBySessionIdAsync(string sessionId);
    Task<TshoppingCart> CreateAsync(TshoppingCart cart);
    Task UpdateAsync(TshoppingCart cart);
    Task<TcartItem?> FindCartItemAsync(int cartId, int productId);
    Task AddCartItemAsync(TcartItem item);
    Task RemoveCartItemAsync(TcartItem item);
    Task ClearCartItemsAsync(int cartId);
}

// File: Sobee.Domain/Repositories/IOrderRepository.cs
public interface IOrderRepository
{
    Task<Torder?> GetByIdAsync(int orderId);
    Task<Torder?> GetByIdWithItemsAsync(int orderId);
    Task<PagedResult<Torder>> GetByUserIdAsync(string userId, int page, int pageSize);
    Task<Torder> CreateAsync(Torder order);
    Task UpdateAsync(Torder order);
    Task AddOrderItemsAsync(IEnumerable<TorderItem> items);
}

// File: Sobee.Domain/Repositories/IProductRepository.cs
public interface IProductRepository
{
    Task<Tproduct?> GetByIdAsync(int productId, bool includeImages = false);
    Task<PagedResult<Tproduct>> SearchAsync(ProductQueryParams query);
    Task<Tproduct> CreateAsync(Tproduct product);
    Task UpdateAsync(Tproduct product);
    Task DeleteAsync(Tproduct product);
    Task<bool> ExistsAsync(int productId);
}
```

### 5.2 DI Registration Pattern

```csharp
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
```

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

```csharp
// File: sobee_API/Mapping/CartProfile.cs
public class CartProfile : Profile
{
    public CartProfile()
    {
        CreateMap<TshoppingCart, CartResponseDto>()
            .ForMember(d => d.Owner, o => o.MapFrom(s => s.UserId != null ? "user" : "guest"))
            .ForMember(d => d.Items, o => o.MapFrom(s => s.TcartItems));

        CreateMap<TcartItem, CartItemResponseDto>()
            .ForMember(d => d.LineTotal, o => o.MapFrom(s => (s.IntQuantity ?? 0) * (s.IntProduct.DecPrice)));
    }
}
```

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
  ```csharp
  public static class StockValidationHelper
  {
      public static (bool IsValid, int Available) ValidateStock(Tproduct product, int requested)
          => (requested <= product.IntStockAmount, product.IntStockAmount);
  }
  ```

- [ ] **3. Extract PromoCalculationHelper** - Move discount calculation (5 lines) to static helper to eliminate duplication.

- [ ] **4. Add Result<T> type** - Create a simple result wrapper for service return types to handle errors without exceptions.
  ```csharp
  public record ServiceResult<T>(T? Value, string? ErrorCode, string? ErrorMessage)
  {
      public bool IsSuccess => ErrorCode == null;
      public static ServiceResult<T> Ok(T value) => new(value, null, null);
      public static ServiceResult<T> Fail(string code, string message) => new(default, code, message);
  }
  ```

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
