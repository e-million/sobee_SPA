# Sobee API Layered Architecture Refactoring Plan

**Created:** 2026-01-22
**Purpose:** Phase-by-phase conversion plan to refactor monolithic ASP.NET Core Web API into Layered Architecture (Controller → Service/Application → Repository/Infrastructure)

---

## Audit Summary

### Key Violations (from Audit Report)

| Finding ID | Severity | Description |
|------------|----------|-------------|
| LAYER-001 | **Blocker** | All 14 domain controllers inject DbContext directly |
| LAYER-002 | **Blocker** | Business logic (checkout, cart merge, promo calc) embedded in controllers |
| LAYER-003 | **Major** | 60+ EF LINQ queries scattered across controllers |
| LAYER-004 | **Major** | Transaction management in controllers (Checkout, PayOrder) |
| LAYER-005 | **Major** | Stock validation duplicated in CartController & OrdersController |
| LAYER-006 | **Major** | Order status transition logic in controller |
| LAYER-007 | **Minor** | DTO projection logic scattered (no centralized mapping) |

### Most Critical Controllers (by risk)

1. **OrdersController** - Checkout workflow (170 lines), transactions, stock decrement, promo snapshot
2. **CartController** - Cart merge logic (60+ lines), promo calculation, stock validation
3. **ProductsController** - Admin CRUD with stock management
4. **AdminAnalyticsController** - 15+ complex queries (read-only, lower risk)

### Top Workflows at Risk

| Workflow | Controller | Risk Level | Why |
|----------|------------|------------|-----|
| Checkout | OrdersController | **Critical** | Transaction, stock, promo, order creation in single method |
| Cart Merge | CartController | **Critical** | Session→User cart transfer, item merging |
| Add to Cart | CartController | **High** | Stock validation, existing item detection |
| Apply Promo | CartController | **High** | Promo validation, usage history |
| Pay Order | OrdersController | **High** | Payment creation, status transition, transaction |
| Cancel Order | OrdersController | **Medium** | Status validation, stock restoration potential |

### Subsystem Migration Order (Justified)

| Priority | Subsystem | Justification |
|----------|-----------|---------------|
| 1 | **Cart** | Foundation for checkout; merge logic is complex & fragile |
| 2 | **Orders/Checkout** | Highest transaction complexity; depends on Cart |
| 3 | **Products** | Admin CRUD; affects Cart/Orders via stock |
| 4 | **Reviews** | Independent; moderate complexity |
| 5 | **Favorites** | Simple; low risk |
| 6 | **Admin Analytics** | Read-only; can parallelize |
| 7 | **Admin Promos/Users** | Low frequency; lower priority |

---

## A) High-Level Roadmap

| Phase | Name | Goal | Subsystems | Why This Order |
|-------|------|------|------------|----------------|
| **0** | Baseline Integration Tests | Lock current behavior before any changes | All critical endpoints | Must detect regressions |
| **1** | Domain Extraction | Extract business rules into testable domain logic | Cart (promo calc, stock validation), Orders (status transitions) | LAYER-002, LAYER-005, LAYER-006 are blockers |
| **2a** | Cart Application Layer | Extract CartService + ICartRepository | Cart | Most complex merge logic; foundation for Orders |
| **2b** | Orders Application Layer | Extract OrderService + IOrderRepository + InventoryService | Orders/Checkout | Depends on Cart patterns; highest transaction risk |
| **2c** | Products Application Layer | Extract ProductService + IProductRepository | Products | Required for inventory consistency |
| **3** | Infrastructure Consolidation | Finalize repositories, optimize queries, add caching hooks | All migrated | LAYER-003 resolution |
| **4** | API Contract Hardening | Centralize error mapping, DTO mapping, validation responses | All | LAYER-007 resolution |
| **5** | Ship Gate | Full regression, flake detection, coverage validation | All | Production readiness |

---

## B) Phase-by-Phase Plan

---

### Phase 0: Baseline Integration Tests

**Goal:** Establish behavioral contracts before any refactoring. All subsequent phases must keep these tests green.

#### 1) Scope

All critical endpoints from audit inventory:

| Subsystem | Endpoints to Cover |
|-----------|--------------------|
| Cart | GET /api/cart, POST /api/cart/items, PUT /api/cart/items/{id}, DELETE /api/cart/items/{id}, DELETE /api/cart, POST /api/cart/promo/apply, DELETE /api/cart/promo |
| Orders | GET /api/orders/{id}, GET /api/orders/my, POST /api/orders/checkout, POST /api/orders/{id}/cancel, POST /api/orders/{id}/pay, PATCH /api/orders/{id}/status |
| Products | GET /api/products, GET /api/products/{id}, POST /api/products, PUT /api/products/{id}, DELETE /api/products/{id} |
| Auth | POST /api/auth/register, /identity/login (Identity endpoints) |

#### 2) Tasks

| # | Task | Files |
|---|------|-------|
| 0.1 | Add Testcontainers.MsSql NuGet package | sobee_API.Tests/sobee_API.Tests.csproj |
| 0.2 | Create `SobeeWebApplicationFactory` with SQL Server container | sobee_API.Tests/Infrastructure/SobeeWebApplicationFactory.cs |
| 0.3 | Create test data seeder for products, users, promos | sobee_API.Tests/Infrastructure/TestDataSeeder.cs |
| 0.4 | Write Cart integration tests (7 endpoints) | sobee_API.Tests/Integration/CartEndpointTests.cs |
| 0.5 | Write Orders integration tests (6 endpoints) | sobee_API.Tests/Integration/OrdersEndpointTests.cs |
| 0.6 | Write Products integration tests (5+ endpoints) | sobee_API.Tests/Integration/ProductsEndpointTests.cs |
| 0.7 | Write Auth integration tests (register, login, 401/403) | sobee_API.Tests/Integration/AuthEndpointTests.cs |
| 0.8 | Document all response contracts (snapshot or assertion) | sobee_API.Tests/Contracts/ |

#### 3) Architecture Rules Enforced

None yet (this phase is observation only).

#### 4) Acceptance Criteria

- [ ] All critical endpoints have at least one happy-path test
- [ ] All critical endpoints have at least one error-path test (400, 401, 404, 409)
- [ ] Tests verify: status code, key response fields, database state changes
- [ ] Tests run against real SQL Server (via Testcontainers)
- [ ] All tests pass on CI

#### 5) Test Gate

**Minimum tests required:**

```
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
```

**Pass criteria:** 100% of baseline tests green.
**Fail action:** Do not proceed to Phase 1. Debug and fix test infrastructure.

#### 6) Risks & Mitigations

| Risk | Mitigation |
|------|------------|
| Existing behavior has bugs we'll lock in | Document known issues; exclude from baseline if intentional |
| Test data conflicts between test runs | Use unique IDs per test; cleanup after each test class |
| Testcontainers startup time | Use shared container per test class; parallelize test classes |

#### 7) Documentation References

- [Integration tests in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests)
- [WebApplicationFactory](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.testing.webapplicationfactory-1)
- [Testcontainers for .NET](https://dotnet.testcontainers.org/)
- [EF Core Testing](https://learn.microsoft.com/en-us/ef/core/testing/)

---

### Phase 1: Domain Extraction

**Goal:** Extract pure business rules into testable domain classes. No HTTP dependencies. No EF dependencies.

#### 1) Scope

| Domain Concept | Current Location | Target |
|----------------|------------------|--------|
| Promo discount calculation | CartController:492-505, OrdersController | `PromoCalculator` static class |
| Stock validation | CartController:84-103, OrdersController:186-207 | `StockValidator` static class |
| Order status transitions | OrdersController:305-320, 418-468 | `OrderStatusMachine` class |
| Cart line total calculation | CartController:ProjectCartAsync | `CartCalculator` static class |

#### 2) Tasks

| # | Task | Files |
|---|------|-------|
| 1.1 | Create Domain folder structure | sobee_API/Domain/ |
| 1.2 | Extract `PromoCalculator.CalculateDiscount(subtotal, discountPercentage)` | sobee_API/Domain/PromoCalculator.cs |
| 1.3 | Extract `StockValidator.Validate(available, requested) -> StockValidationResult` | sobee_API/Domain/StockValidator.cs |
| 1.4 | Extract `OrderStatusMachine.CanTransition(from, to)` and `GetAllowedTransitions(from)` | sobee_API/Domain/OrderStatusMachine.cs |
| 1.5 | Extract `CartCalculator.CalculateSubtotal(items)` and `CalculateTotal(subtotal, discount)` | sobee_API/Domain/CartCalculator.cs |
| 1.6 | Create `ServiceResult<T>` for error handling without exceptions | sobee_API/Domain/ServiceResult.cs |
| 1.7 | Update CartController to use domain classes (keep DbContext for now) | sobee_API/Controllers/CartController.cs |
| 1.8 | Update OrdersController to use domain classes (keep DbContext for now) | sobee_API/Controllers/OrdersController.cs |
| 1.9 | Write unit tests for all domain classes | sobee_API.Tests/Domain/*.cs |

#### 3) Architecture Rules Enforced

- [ ] Domain classes have NO dependencies on EF Core (`Microsoft.EntityFrameworkCore.*`)
- [ ] Domain classes have NO dependencies on ASP.NET Core (`Microsoft.AspNetCore.*`)
- [ ] Domain classes are pure functions or simple state machines
- [ ] Domain classes use primitive types or simple DTOs as inputs/outputs

#### 4) Acceptance Criteria

- [ ] `grep -r "using Microsoft.EntityFrameworkCore" sobee_API/Domain/` returns nothing
- [ ] `grep -r "using Microsoft.AspNetCore" sobee_API/Domain/` returns nothing
- [ ] All domain classes have >90% branch coverage in unit tests
- [ ] All Phase 0 integration tests still pass (behavior unchanged)

#### 5) Test Gate

**Domain unit tests required:**

```
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
```

**Pass criteria:** All domain unit tests pass + all Phase 0 integration tests pass.
**Fail action:** Fix domain logic or controller integration before proceeding.

#### 6) Risks & Mitigations

| Risk | Mitigation |
|------|------------|
| Calculation logic differs from original | Compare domain class output with hardcoded expected values from current behavior |
| Status transitions have edge cases | Map all transitions from OrderStatuses.cs constants; test exhaustively |
| Changing controller code breaks behavior | Run Phase 0 tests after each controller modification |

---

### Phase 2a: Cart Application Layer

**Goal:** Extract CartService and ICartRepository. Controllers become thin HTTP handlers.

#### 1) Scope

| Endpoint | Current Handler | Target Service Method |
|----------|-----------------|----------------------|
| GET /api/cart | CartController.GetCart | ICartService.GetCartAsync |
| POST /api/cart/items | CartController.AddItem | ICartService.AddItemAsync |
| PUT /api/cart/items/{id} | CartController.UpdateItem | ICartService.UpdateItemAsync |
| DELETE /api/cart/items/{id} | CartController.RemoveItem | ICartService.RemoveItemAsync |
| DELETE /api/cart | CartController.ClearCart | ICartService.ClearCartAsync |
| POST /api/cart/promo/apply | CartController.ApplyPromo | ICartService.ApplyPromoAsync |
| DELETE /api/cart/promo | CartController.RemovePromo | ICartService.RemovePromoAsync |

#### 2) Tasks

| # | Task | Files |
|---|------|-------|
| 2a.1 | Create service interfaces folder | sobee_API/Services/Interfaces/ |
| 2a.2 | Define `ICartService` interface | sobee_API/Services/Interfaces/ICartService.cs |
| 2a.3 | Define `ICartRepository` interface | Sobee.Domain/Repositories/ICartRepository.cs |
| 2a.4 | Define `IPromoRepository` interface | Sobee.Domain/Repositories/IPromoRepository.cs |
| 2a.5 | Define `IProductRepository` interface (for stock checks) | Sobee.Domain/Repositories/IProductRepository.cs |
| 2a.6 | Implement `CartRepository` | Sobee.Domain/Repositories/CartRepository.cs |
| 2a.7 | Implement `PromoRepository` | Sobee.Domain/Repositories/PromoRepository.cs |
| 2a.8 | Implement `ProductRepository` (partial, for stock) | Sobee.Domain/Repositories/ProductRepository.cs |
| 2a.9 | Implement `CartService` with full cart logic | sobee_API/Services/CartService.cs |
| 2a.10 | Register services in DI | sobee_API/Program.cs |
| 2a.11 | Refactor `CartController` to use `ICartService` | sobee_API/Controllers/CartController.cs |
| 2a.12 | Remove `_db` field from `CartController` | sobee_API/Controllers/CartController.cs |
| 2a.13 | Write unit tests for `CartService` | sobee_API.Tests/Services/CartServiceTests.cs |
| 2a.14 | Write repository integration tests | sobee_API.Tests/Repositories/CartRepositoryTests.cs |

#### 3) Architecture Rules Enforced

- [ ] `CartController` does not reference `SobeecoredbContext`
- [ ] `CartController` does not contain `await _db.` calls
- [ ] `CartService` does not reference `ControllerBase`, `IActionResult`, `HttpContext`
- [ ] `CartService` returns `ServiceResult<CartDto>` not `IActionResult`
- [ ] `CartRepository` does not contain business logic (only query shaping)

#### 4) Acceptance Criteria

- [ ] `grep "SobeecoredbContext" sobee_API/Controllers/CartController.cs` returns nothing
- [ ] `grep "IActionResult" sobee_API/Services/CartService.cs` returns nothing
- [ ] All 7 cart endpoints work identically (Phase 0 tests pass)
- [ ] CartService has unit tests with mocked repositories
- [ ] CartRepository has integration tests against SQL Server

#### 5) Test Gate

**Application unit tests (CartService):**

```
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
```

**Repository integration tests:**

```
CartRepositoryTests:
  - FindByUserIdAsync_Exists_ReturnsCart
  - FindByUserIdAsync_NotExists_ReturnsNull
  - FindBySessionIdAsync_Exists_ReturnsCart
  - CreateAsync_CreatesCart
  - AddCartItemAsync_AddsItem
  - UpdateCartItemAsync_UpdatesQuantity
  - RemoveCartItemAsync_RemovesItem
  - ClearCartItemsAsync_RemovesAllItems
```

**Pass criteria:** All unit tests + repository tests + Phase 0 integration tests pass.
**Fail action:** Debug service/repository interaction; verify DI registration.

#### 6) Risks & Mitigations

| Risk | Mitigation |
|------|------------|
| Cart merge logic has subtle edge cases | Dedicated tests for each merge scenario (guest->user, user has cart, user has no cart) |
| Transaction boundaries differ | CartService uses single SaveChanges; verify atomicity in tests |
| GuestSessionService integration | Keep GuestSessionService as infrastructure service; inject into CartService |

---

### Phase 2b: Orders Application Layer

**Goal:** Extract OrderService, IOrderRepository, and InventoryService. Handle complex checkout workflow.

#### 1) Scope

| Endpoint | Current Handler | Target Service Method |
|----------|-----------------|----------------------|
| GET /api/orders/{id} | OrdersController.GetOrder | IOrderService.GetOrderAsync |
| GET /api/orders/my | OrdersController.GetMyOrders | IOrderService.GetUserOrdersAsync |
| POST /api/orders/checkout | OrdersController.Checkout | IOrderService.CheckoutAsync |
| POST /api/orders/{id}/cancel | OrdersController.CancelOrder | IOrderService.CancelOrderAsync |
| POST /api/orders/{id}/pay | OrdersController.PayOrder | IOrderService.PayOrderAsync |
| PATCH /api/orders/{id}/status | OrdersController.UpdateOrderStatus | IOrderService.UpdateStatusAsync |

#### 2) Tasks

| # | Task | Files |
|---|------|-------|
| 2b.1 | Define `IOrderService` interface | sobee_API/Services/Interfaces/IOrderService.cs |
| 2b.2 | Define `IOrderRepository` interface | Sobee.Domain/Repositories/IOrderRepository.cs |
| 2b.3 | Define `IInventoryService` interface | sobee_API/Services/Interfaces/IInventoryService.cs |
| 2b.4 | Define `IPaymentRepository` interface | Sobee.Domain/Repositories/IPaymentRepository.cs |
| 2b.5 | Implement `OrderRepository` | Sobee.Domain/Repositories/OrderRepository.cs |
| 2b.6 | Implement `PaymentRepository` | Sobee.Domain/Repositories/PaymentRepository.cs |
| 2b.7 | Implement `InventoryService` (stock validation + decrement) | sobee_API/Services/InventoryService.cs |
| 2b.8 | Implement `OrderService` with checkout workflow | sobee_API/Services/OrderService.cs |
| 2b.9 | Move transaction management to `OrderService.CheckoutAsync` | sobee_API/Services/OrderService.cs |
| 2b.10 | Register services in DI | sobee_API/Program.cs |
| 2b.11 | Refactor `OrdersController` to use `IOrderService` | sobee_API/Controllers/OrdersController.cs |
| 2b.12 | Remove `_db` field from `OrdersController` | sobee_API/Controllers/OrdersController.cs |
| 2b.13 | Write unit tests for `OrderService` | sobee_API.Tests/Services/OrderServiceTests.cs |
| 2b.14 | Write unit tests for `InventoryService` | sobee_API.Tests/Services/InventoryServiceTests.cs |
| 2b.15 | Write repository integration tests | sobee_API.Tests/Repositories/OrderRepositoryTests.cs |

#### 3) Architecture Rules Enforced

- [ ] `OrdersController` does not reference `SobeecoredbContext`
- [ ] `OrdersController` does not contain transaction code (`BeginTransactionAsync`)
- [ ] `OrderService` manages transactions via `IDbContextTransaction` or Unit of Work
- [ ] `OrderService` uses `ICartService` (not ICartRepository directly) for cart operations
- [ ] `InventoryService` is the single source of truth for stock operations

#### 4) Acceptance Criteria

- [ ] `grep "SobeecoredbContext" sobee_API/Controllers/OrdersController.cs` returns nothing
- [ ] `grep "BeginTransactionAsync" sobee_API/Controllers/OrdersController.cs` returns nothing
- [ ] Checkout creates order + decrements stock + clears cart atomically
- [ ] All 6 orders endpoints work identically (Phase 0 tests pass)
- [ ] OrderService has unit tests with mocked dependencies

#### 5) Test Gate

**Application unit tests (OrderService):**

```
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
```

**Repository integration tests:**

```
OrderRepositoryTests:
  - GetByIdAsync_Exists_ReturnsOrder
  - GetByIdWithItemsAsync_IncludesItems
  - GetByUserIdAsync_ReturnsPaginated
  - CreateAsync_CreatesOrderWithItems
  - UpdateAsync_UpdatesStatus
```

**Pass criteria:** All unit tests + repository tests + Phase 0 integration tests pass.
**Fail action:** Verify transaction boundaries; check stock decrement atomicity.

#### 6) Risks & Mitigations

| Risk | Mitigation |
|------|------------|
| Transaction rollback doesn't restore stock | Test explicitly that failed checkout leaves stock unchanged |
| Concurrent checkouts cause overselling | Add integration test with parallel checkout attempts |
| Promo snapshot logic differs | Verify promo fields on created order match original behavior |

---

### Phase 2c: Products Application Layer

**Goal:** Extract ProductService. Complete IProductRepository implementation.

#### 1) Scope

| Endpoint | Target Service Method |
|----------|----------------------|
| GET /api/products | IProductService.GetProductsAsync |
| GET /api/products/{id} | IProductService.GetProductAsync |
| POST /api/products | IProductService.CreateProductAsync |
| PUT /api/products/{id} | IProductService.UpdateProductAsync |
| DELETE /api/products/{id} | IProductService.DeleteProductAsync |
| POST /api/products/{id}/images | IProductService.AddProductImageAsync |
| DELETE /api/products/{id}/images/{imgId} | IProductService.DeleteProductImageAsync |

#### 2) Tasks

| # | Task | Files |
|---|------|-------|
| 2c.1 | Define `IProductService` interface | sobee_API/Services/Interfaces/IProductService.cs |
| 2c.2 | Complete `IProductRepository` with search/filter | Sobee.Domain/Repositories/IProductRepository.cs |
| 2c.3 | Implement `ProductRepository` (full) | Sobee.Domain/Repositories/ProductRepository.cs |
| 2c.4 | Implement `ProductService` | sobee_API/Services/ProductService.cs |
| 2c.5 | Register services in DI | sobee_API/Program.cs |
| 2c.6 | Refactor `ProductsController` | sobee_API/Controllers/ProductsController.cs |
| 2c.7 | Write unit tests | sobee_API.Tests/Services/ProductServiceTests.cs |
| 2c.8 | Write repository integration tests | sobee_API.Tests/Repositories/ProductRepositoryTests.cs |

#### 3) Architecture Rules Enforced

- [ ] `ProductsController` does not reference `SobeecoredbContext`
- [ ] `ProductService` handles admin authorization checks
- [ ] `ProductRepository` handles complex search/filter/sort queries

#### 4) Acceptance Criteria

- [ ] All 7 product endpoints work identically
- [ ] Search, filter, sort, pagination work correctly
- [ ] Admin-only endpoints reject non-admin users

#### 5) Test Gate

```
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
```

**Pass criteria:** All tests pass.

---

### Phase 3: Infrastructure Consolidation

**Goal:** Finalize repository layer. Optimize queries. Add query specifications if needed.

#### 1) Scope

- All remaining controllers (Reviews, Favorites, Admin*)
- Query optimization across all repositories
- Caching infrastructure (if needed)

#### 2) Tasks

| # | Task | Files |
|---|------|-------|
| 3.1 | Extract ReviewService + IReviewRepository | sobee_API/Services/ReviewService.cs, Sobee.Domain/Repositories/ReviewRepository.cs |
| 3.2 | Extract FavoriteService + IFavoriteRepository | sobee_API/Services/FavoriteService.cs, Sobee.Domain/Repositories/FavoriteRepository.cs |
| 3.3 | Extract AdminPromoService | sobee_API/Services/AdminPromoService.cs |
| 3.4 | Extract AdminUserService | sobee_API/Services/AdminUserService.cs |
| 3.5 | Extract AnalyticsService + IAnalyticsRepository | sobee_API/Services/AnalyticsService.cs |
| 3.6 | Refactor all remaining controllers | sobee_API/Controllers/*.cs |
| 3.7 | Audit all repositories for N+1 queries | All repositories |
| 3.8 | Add `.AsNoTracking()` to read-only queries | All repositories |
| 3.9 | Add query result caching (if applicable) | Repository decorators or service layer |
| 3.10 | Write integration tests for all repositories | sobee_API.Tests/Repositories/*.cs |

#### 3) Architecture Rules Enforced

- [ ] No controller references `SobeecoredbContext` (zero violations)
- [ ] All EF queries are in repositories
- [ ] Read-only queries use `AsNoTracking()`
- [ ] Complex queries use projection to DTOs (not loading full entities)

#### 4) Acceptance Criteria

- [ ] `grep -r "SobeecoredbContext" sobee_API/Controllers/` returns nothing
- [ ] `grep -r "ApplicationDbContext" sobee_API/Controllers/` returns nothing (except AdminUsersController if using UserManager)
- [ ] No N+1 queries detected in common operations
- [ ] All Phase 0 integration tests pass

#### 5) Test Gate

**Integration tests for real SQL Server behavior:**

```
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
```

**Pass criteria:** All integration tests pass with real SQL Server.

#### 6) Risks & Mitigations

| Risk | Mitigation |
|------|------------|
| Analytics queries are complex and slow | Profile with SQL Server profiler; add indexes if needed |
| AdminUsersController uses UserManager directly | This is acceptable; UserManager is the Identity abstraction |

---

### Phase 4: API Contract Hardening

**Goal:** Standardize error responses, DTO mapping, validation responses, and serialization contracts.

#### 1) Scope

- Error response standardization
- DTO mapping centralization (AutoMapper or manual)
- Validation response format
- API versioning consideration

#### 2) Tasks

| # | Task | Files |
|---|------|-------|
| 4.1 | Add AutoMapper (optional) or create mapping extension methods | sobee_API/Mapping/*.cs |
| 4.2 | Centralize error mapping in `ApiControllerBase` or middleware | sobee_API/Controllers/ApiControllerBase.cs |
| 4.3 | Standardize `ServiceResult<T>` to HTTP response mapping | sobee_API/Extensions/ServiceResultExtensions.cs |
| 4.4 | Add API response contracts (OpenAPI schemas) | sobee_API/Contracts/*.cs |
| 4.5 | Add contract tests for response shapes | sobee_API.Tests/Contracts/*.cs |
| 4.6 | Verify all error codes are documented | sobee_API/DTOs/Common/ErrorCodes.cs |

#### 3) Architecture Rules Enforced

- [ ] All error responses use `ApiErrorResponse` format
- [ ] All validation errors return consistent shape
- [ ] All DTOs have explicit mapping (no implicit entity exposure)

#### 4) Acceptance Criteria

- [ ] All 4xx/5xx responses match `ApiErrorResponse` schema
- [ ] Validation errors include field-level details
- [ ] No entity types exposed in API responses
- [ ] OpenAPI spec accurately reflects actual responses

#### 5) Test Gate

```
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
```

**Pass criteria:** All contract tests pass.

---

### Phase 5: Ship Gate

**Goal:** Validate production readiness. Zero flakes. Full coverage. Regression tests for any bugs found.

#### 1) Scope

- Full test suite execution
- Flake detection
- Coverage analysis
- Performance baseline

#### 2) Tasks

| # | Task | Files |
|---|------|-------|
| 5.1 | Run full test suite 10x consecutively | CI pipeline |
| 5.2 | Identify and fix any flaky tests | Various |
| 5.3 | Generate code coverage report | CI pipeline |
| 5.4 | Verify critical paths have unit + integration coverage | Coverage report |
| 5.5 | Add performance baseline tests (optional) | sobee_API.Tests/Performance/*.cs |
| 5.6 | Document architecture in ARCHITECTURE.md | sobee_API/ARCHITECTURE.md |
| 5.7 | Update CLAUDE.md with layer rules | CLAUDE.md |

#### 3) Acceptance Criteria

- [ ] 10 consecutive test runs with 0 failures
- [ ] >80% line coverage on Services
- [ ] >70% line coverage on Repositories
- [ ] All critical workflows (Checkout, Cart Merge, Pay Order) have both unit and integration tests
- [ ] Every bug fixed during refactor has a regression test

#### 4) Test Gate

**Ship criteria:**
- [ ] All Phase 0-4 tests pass
- [ ] Zero flaky tests in 10 runs
- [ ] Coverage thresholds met
- [ ] No known regressions

---

## C) Test Strategy & Tooling Recommendations

### Frameworks

| Purpose | Recommendation |
|---------|----------------|
| Unit testing | **xUnit** (industry standard, good async support) |
| Mocking | **NSubstitute** or **Moq** (NSubstitute preferred for cleaner syntax) |
| Assertions | **FluentAssertions** (readable, chainable) |
| Integration testing | **Microsoft.AspNetCore.Mvc.Testing** (WebApplicationFactory) |
| Database | **Testcontainers.MsSql** (real SQL Server in Docker) |
| Coverage | **Coverlet** + **ReportGenerator** |

### Project Structure

```
sobee_API.Tests/
├── Infrastructure/
│   ├── SobeeWebApplicationFactory.cs      # WebApplicationFactory with Testcontainers
│   ├── TestDataSeeder.cs                  # Seed products, users, promos
│   ├── TestAuthHandler.cs                 # Fake auth for testing
│   └── DatabaseFixture.cs                 # Shared container fixture
├── Domain/
│   ├── PromoCalculatorTests.cs
│   ├── StockValidatorTests.cs
│   ├── OrderStatusMachineTests.cs
│   └── CartCalculatorTests.cs
├── Services/
│   ├── CartServiceTests.cs
│   ├── OrderServiceTests.cs
│   ├── InventoryServiceTests.cs
│   └── ProductServiceTests.cs
├── Repositories/
│   ├── CartRepositoryTests.cs
│   ├── OrderRepositoryTests.cs
│   └── ProductRepositoryTests.cs
├── Integration/
│   ├── CartEndpointTests.cs
│   ├── OrdersEndpointTests.cs
│   ├── ProductsEndpointTests.cs
│   └── AuthEndpointTests.cs
└── Contracts/
    ├── ErrorResponseContractTests.cs
    └── SerializationContractTests.cs
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
|---------|----------|
| Database state | Each test class gets fresh seeded data; use transactions that rollback |
| User context | Use test auth handler with configurable claims |
| Time | Inject `TimeProvider`; use `FakeTimeProvider` in tests |
| External services | Not applicable (no external dependencies noted) |
| Parallel execution | Separate database per test class (or use transactions) |

### Avoiding Flaky Tests

1. **No shared mutable state** between tests
2. **Unique identifiers** per test (GUIDs for emails, product names)
3. **Explicit waits** only where truly async (not arbitrary delays)
4. **Deterministic time** via `TimeProvider`
5. **Fresh database** per test class
6. **No dependency on test execution order**

### Documentation References

- [ASP.NET Core Integration Testing](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests)
- [EF Core Testing with Real Database](https://learn.microsoft.com/en-us/ef/core/testing/testing-with-the-database)
- [Testcontainers .NET](https://dotnet.testcontainers.org/modules/mssql/)
- [xUnit Documentation](https://xunit.net/docs/getting-started/netcore/cmdline)
- [FluentAssertions](https://fluentassertions.com/introduction)
- [TimeProvider in .NET 8](https://learn.microsoft.com/en-us/dotnet/api/system.timeprovider)

---

## D) Traceability Matrix

### Audit Finding -> Phase -> Tests

| Finding ID | Description | Resolved In | Verification Tests |
|------------|-------------|-------------|-------------------|
| LAYER-001 | Controllers inject DbContext | Phase 2a, 2b, 2c, 3 | Grep check + all integration tests |
| LAYER-002 | Business logic in controllers | Phase 1, 2a, 2b | Domain unit tests + service unit tests |
| LAYER-003 | EF queries in controllers | Phase 2a, 2b, 2c, 3 | Repository integration tests |
| LAYER-004 | Transactions in controllers | Phase 2b | OrderServiceTests.CheckoutAsync_RollsBackOnFailure |
| LAYER-005 | Stock validation duplicated | Phase 1, 2b | StockValidatorTests, InventoryServiceTests |
| LAYER-006 | Status transition in controller | Phase 1 | OrderStatusMachineTests |
| LAYER-007 | DTO mapping scattered | Phase 4 | ContractTests |

### Endpoint -> Phase -> Test Coverage

| Endpoint | Phase Migrated | Unit Test | Integration Test |
|----------|---------------|-----------|------------------|
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

---

## E) Definition of Layered Compliance Checklist

Run this checklist after each phase to verify compliance:

### Controller Layer Compliance

- [ ] No controller file contains `SobeecoredbContext` or `ApplicationDbContext` (except through Identity's UserManager)
- [ ] No controller file contains `await _db.` or `_context.`
- [ ] No controller file contains `BeginTransactionAsync` or `SaveChangesAsync`
- [ ] No controller file contains EF-specific types (`DbSet`, `IQueryable<TEntity>`)
- [ ] All controller methods are <=30 lines (excluding attributes and braces)
- [ ] Controllers only use: routing, model binding, validation, auth, and response mapping

**Verification command:**
```bash
grep -rn "SobeecoredbContext\|ApplicationDbContext\|\.SaveChanges\|BeginTransaction\|DbSet<" sobee_API/Controllers/
# Expected: No results (or only UserManager-related in Auth controllers)
```

### Service Layer Compliance

- [ ] No service file contains `ControllerBase`, `IActionResult`, `ActionResult`, `HttpContext`
- [ ] No service file contains `[Http*]` attributes
- [ ] All services return `ServiceResult<T>` or domain DTOs
- [ ] Services inject repositories via interfaces (not DbContext directly)
- [ ] Services contain business logic (validation, calculations, workflow orchestration)

**Verification command:**
```bash
grep -rn "IActionResult\|ActionResult\|HttpContext\|ControllerBase" sobee_API/Services/
# Expected: No results
```

### Repository Layer Compliance

- [ ] Repositories only contain data access logic (no business rules)
- [ ] Repositories use `AsNoTracking()` for read-only queries
- [ ] Repositories return entities or projections (not DTOs with business logic)
- [ ] No pricing, discount, or status transition logic in repositories

**Verification command:**
```bash
grep -rn "CalculateDiscount\|CanTransition\|ValidateStock" Sobee.Domain/Repositories/
# Expected: No results
```

### Domain Layer Compliance

- [ ] Domain classes have no EF Core references
- [ ] Domain classes have no ASP.NET Core references
- [ ] Domain classes are pure functions or simple state machines
- [ ] Domain classes have >90% unit test coverage

**Verification command:**
```bash
grep -rn "Microsoft.EntityFrameworkCore\|Microsoft.AspNetCore" sobee_API/Domain/
# Expected: No results
```

### Test Compliance

- [ ] All Phase 0 baseline tests pass
- [ ] All domain unit tests pass
- [ ] All service unit tests pass
- [ ] All repository integration tests pass
- [ ] All endpoint integration tests pass
- [ ] Zero flaky tests in 5 consecutive runs

---

## Summary

This plan provides a structured, test-gated approach to refactoring the Sobee API from a monolithic controller-centric architecture to a proper layered architecture. The key principles are:

1. **Lock behavior first** (Phase 0) before any changes
2. **Extract domain logic** (Phase 1) into testable pure functions
3. **Migrate vertically** (Phase 2a-c) by subsystem to minimize blast radius
4. **Consolidate infrastructure** (Phase 3) after patterns are established
5. **Harden contracts** (Phase 4) for API stability
6. **Validate thoroughly** (Phase 5) before shipping

Each phase has explicit acceptance criteria and test gates that must pass before proceeding. This ensures behavior stability throughout the refactor and builds confidence in the final architecture.

---

**Document Created:** 2026-01-22
**Based On:** ARCHITECTURE_AUDIT_REPORT.md
