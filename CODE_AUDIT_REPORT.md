# Sobee SPA Code Audit Report

> **Audit Date**: January 2026
> **Backend**: ASP.NET Core 8 / EF Core
> **Frontend**: Angular 20 / TypeScript / Tailwind CSS

---

## 1. Critical Issues (Security/Performance)

### 1.1 N+1 Query Risk in ReviewsController

- **Location**: [ReviewsController.cs:38-63](sobee_API/sobee_API/Controllers/ReviewsController.cs#L38-L63)
- **Issue**: Nested query inside `Select()` causes N+1 queries. For each review, a separate query is executed to fetch replies.
- **Fix**:
```csharp
// Current (N+1 risk):
.Select(r => new
{
    // ...
    replies = _db.TReviewReplies
        .Where(rr => rr.IntReviewId == r.IntReviewId)
        .OrderBy(rr => rr.created_at)
        .Select(rr => new { /* ... */ })
        .ToList()
})

// Fixed (eager load with Include):
var reviews = await _db.Treviews
    .Include(r => r.TReviewReplies)  // Add navigation property
    .Where(r => r.IntProductId == productId)
    .OrderByDescending(r => r.DtmReviewDate)
    .AsNoTracking()
    .ToListAsync();

// Then project in-memory
var result = reviews.Select(r => new
{
    // ...fields...
    replies = r.TReviewReplies
        .OrderBy(rr => rr.created_at)
        .Select(rr => new { /* ... */ })
        .ToList()
});
```

---

### 1.2 Missing AsNoTracking on Read-Only Queries

- **Location**: [CartController.cs:346-351](sobee_API/sobee_API/Controllers/CartController.cs#L346-L351)
- **Issue**: `FindCartAsync` performs read-only lookups without `AsNoTracking()`, causing unnecessary change tracking overhead.
- **Fix**:
```csharp
// Current:
return await _db.TshoppingCarts.FirstOrDefaultAsync(c => c.UserId == userId);

// Fixed:
return await _db.TshoppingCarts
    .AsNoTracking()
    .FirstOrDefaultAsync(c => c.UserId == userId);
```

---

### 1.3 Potential Unbounded Query in GetOrdersPerDay

- **Location**: [AdminController.cs:60-81](sobee_API/sobee_API/Controllers/AdminController.cs#L60-L81)
- **Issue**: While `days` parameter has a max of 365, grouping by `DtmOrderDate` (which may include time component) could produce unexpected results. Also, large date ranges could be expensive.
- **Fix**:
```csharp
// Group by date only (truncate time):
.GroupBy(o => o.DtmOrderDate.Date)  // or use EF.Functions.DateDiffDay
```

---

### 1.4 422 Validation Errors Show Toast + Form Errors (Duplicate UX)

- **Location**: [error.interceptor.ts:58-60](sobee_Client/src/app/core/interceptors/error.interceptor.ts#L58-L60)
- **Issue**: When a 422 is returned, the interceptor shows a generic toast ("Validation failed. Please check your input."), but the component also displays field-level errors. This creates duplicate error messaging.
- **Fix**:
```typescript
// Suppress toast for 422 when field errors are present:
case 422:
  // Let components handle field-level validation errors
  if (error.error?.details?.errors) {
    showToast = false;  // Suppress generic toast
  }
  errorMessage = error.error?.message || 'Validation failed. Please check your input.';
  break;
```

---

### 1.5 Missing Image Dimensions Cause Layout Shift

- **Location**: [product-card.html:8-11](sobee_Client/src/app/shared/components/product-card/product-card.html#L8-L11)
- **Issue**: Product images don't have explicit `width` and `height` attributes, causing Cumulative Layout Shift (CLS) as images load.
- **Fix**:
```html
<!-- Current: -->
<img
  [src]="productImage"
  [alt]="product.name ?? 'Sobee product'"
  class="w-full h-full object-cover ..."
/>

<!-- Fixed (add dimensions to prevent layout shift): -->
<img
  [src]="productImage"
  [alt]="product.name ?? 'Sobee product'"
  width="300"
  height="300"
  loading="lazy"
  class="w-full h-full object-cover ..."
/>
```

---

### 1.6 Cart Item Images Not Loaded

- **Location**: [cart.html:53-55](sobee_Client/src/app/features/cart/cart.html#L53-L55)
- **Issue**: Cart items display a placeholder "No image" instead of actual product images. The `CartItemResponseDto` includes product info, but image URLs are not being utilized.
- **Fix**: Update `CartProductDto` to include `primaryImageUrl` and display it in the template:
```html
<!-- Current: -->
<div class="w-24 h-24 bg-slate-100 rounded-lg flex items-center justify-center text-xs text-slate-400">
  No image
</div>

<!-- Fixed: -->
@if (item.product?.primaryImageUrl) {
  <img
    [src]="item.product.primaryImageUrl"
    [alt]="item.product?.name"
    width="96"
    height="96"
    class="w-24 h-24 rounded-lg object-cover"
  />
} @else {
  <div class="w-24 h-24 bg-slate-100 rounded-lg flex items-center justify-center text-xs text-slate-400">
    No image
  </div>
}
```

---

### 1.7 No Correlation ID in Frontend Error Logs

- **Location**: [error.interceptor.ts:78](sobee_Client/src/app/core/interceptors/error.interceptor.ts#L78)
- **Issue**: The backend returns `X-Correlation-Id` headers, but the frontend error interceptor doesn't capture or log them for debugging support tickets.
- **Fix**:
```typescript
// Extract correlation ID from response headers:
const correlationId = error.headers?.get('X-Correlation-Id');
console.error('HTTP Error:', errorMessage, {
  correlationId,
  status: error.status,
  url: req.url
});

// Optionally include in error toast for support:
if (showToast && correlationId) {
  toastService.error(`${errorMessage} (Ref: ${correlationId.slice(0, 8)})`);
}
```

---

## 2. Architectural Improvements (.NET 8 / Angular Standards)

### 2.1 Use Primary Constructors (C# 12)

- **Concept**: C# 12 primary constructors reduce boilerplate in controllers
- **Location**: [CartController.cs:17-30](sobee_API/sobee_API/Controllers/CartController.cs#L17-L30)
- **Current Code**:
```csharp
public class CartController : ControllerBase
{
    private readonly SobeecoredbContext _db;
    private readonly GuestSessionService _guestSessionService;
    private readonly RequestIdentityResolver _identityResolver;

    public CartController(
        SobeecoredbContext db,
        GuestSessionService guestSessionService,
        RequestIdentityResolver identityResolver)
    {
        _db = db;
        _guestSessionService = guestSessionService;
        _identityResolver = identityResolver;
    }
```
- **Refactored Code**:
```csharp
public class CartController(
    SobeecoredbContext db,
    GuestSessionService guestSessionService,
    RequestIdentityResolver identityResolver) : ControllerBase
{
    // Use parameters directly: db, guestSessionService, identityResolver
```

---

### 2.2 Use Collection Expressions (C# 12)

- **Concept**: Replace `new List<>()` with collection expressions
- **Location**: [ProductsController.cs:137](sobee_API/sobee_API/Controllers/ProductsController.cs#L137)
- **Current Code**:
```csharp
images = (product.TproductImages ?? new List<TproductImage>())
```
- **Refactored Code**:
```csharp
images = (product.TproductImages ?? [])
```

---

### 2.3 Missing ChangeDetectionStrategy.OnPush

- **Concept**: Components with signal-based state should use OnPush for performance
- **Location**: [shop.ts:12-17](sobee_Client/src/app/features/shop/shop.ts#L12-L17), [home.ts:11-16](sobee_Client/src/app/features/home/home.ts#L11-L16), [admin-dashboard.ts:24-29](sobee_Client/src/app/features/admin/dashboard/admin-dashboard.ts#L24-L29)
- **Current Code**:
```typescript
@Component({
  selector: 'app-shop',
  imports: [CommonModule, RouterModule, FormsModule, MainLayout, ProductCard],
  templateUrl: './shop.html',
  styleUrl: './shop.css'
})
```
- **Refactored Code**:
```typescript
import { ChangeDetectionStrategy } from '@angular/core';

@Component({
  selector: 'app-shop',
  imports: [CommonModule, RouterModule, FormsModule, MainLayout, ProductCard],
  templateUrl: './shop.html',
  styleUrl: './shop.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
```

**Components that should add OnPush**:
| Component | File | Reason |
|-----------|------|--------|
| Shop | shop.ts | Uses signals for all state |
| Home | home.ts | Uses signals for all state |
| AdminDashboard | admin-dashboard.ts | Uses signals for all state |
| Cart | cart.ts | Uses signals for all state |
| Checkout | checkout.ts | Uses signals for all state |
| ProductCard | product-card.ts | Pure presentational component |
| Navbar | navbar.ts | Uses signals + takeUntilDestroyed |

---

### 2.4 Subscription Cleanup Already Handled Well

- **Location**: [navbar.ts:37-57](sobee_Client/src/app/shared/components/navbar/navbar.ts#L37-L57)
- **Assessment**: The Navbar correctly uses `takeUntilDestroyed(this.destroyRef)` for the search Subject subscription. This is the recommended modern Angular pattern.
- **No change needed** - This is exemplary code.

---

### 2.5 Inconsistent State Management Pattern

- **Concept**: Some components mix signals and plain properties
- **Location**: [shop.ts:28-38](sobee_Client/src/app/features/shop/shop.ts#L28-L38)
- **Current Code**:
```typescript
// Signals for reactive state
products = signal<Product[]>([]);
allProducts = signal<Product[]>([]);
loading = signal(true);

// Plain properties for form state
selectedSort = 'newest';
selectedCategory = 'all';
minPrice = '';
maxPrice = '';
```
- **Assessment**: This is actually a reasonable pattern - signals for derived/reactive state, plain properties for form bindings. However, for full consistency, consider converting form state to signals too:
```typescript
selectedSort = signal('newest');
selectedCategory = signal('all');
minPrice = signal('');
maxPrice = signal('');
```

---

### 2.6 API URL Inconsistency

- **Location**: [auth.service.ts:22-23](sobee_Client/src/app/core/services/auth.service.ts#L22-L23)
- **Issue**: Service uses two different base URLs (`apiUrl` and `apiBaseUrl`):
```typescript
private readonly apiUrl = environment.apiBaseUrl;  // Used for /login
private readonly meUrl = `${environment.apiUrl}/me`;  // Uses different property
```
- **Fix**: Standardize on a single API URL pattern:
```typescript
private readonly apiUrl = environment.apiUrl;  // Single source
```

---

### 2.7 Centralize Error Response Helpers

- **Concept**: Multiple controllers duplicate error helper methods
- **Location**: [CartController.cs:550-566](sobee_API/sobee_API/Controllers/CartController.cs#L550-L566), [OrdersController.cs:552-568](sobee_API/sobee_API/Controllers/OrdersController.cs#L552-L568)
- **Issue**: Both controllers define identical `BadRequestError`, `NotFoundError`, `ConflictError`, etc. helper methods.
- **Fix**: Create a base controller or extension methods:
```csharp
// Create: Controllers/ApiControllerBase.cs
public abstract class ApiControllerBase : ControllerBase
{
    protected BadRequestObjectResult BadRequestError(string message, string? code = null, object? details = null)
        => BadRequest(new ApiErrorResponse(message, code, details));

    protected NotFoundObjectResult NotFoundError(string message, string? code = null, object? details = null)
        => NotFound(new ApiErrorResponse(message, code, details));

    // ... other helpers
}

// Then:
public class CartController : ApiControllerBase { }
public class OrdersController : ApiControllerBase { }
```

---

## 3. UI/UX Refinements

### 3.1 Mobile Menu Accessibility

- **Location**: [navbar.html](sobee_Client/src/app/shared/components/navbar/navbar.html) (file not fully read, but pattern detected in navbar.ts)
- **Issue**: Mobile menu toggle button likely missing `aria-expanded` and `aria-controls` attributes.
- **Suggestion**:
```html
<button
  (click)="toggleMobileMenu()"
  [attr.aria-expanded]="mobileMenuOpen()"
  aria-controls="mobile-menu"
  aria-label="Toggle navigation menu"
  class="..."
>
  <!-- hamburger icon -->
</button>

<nav id="mobile-menu" [class.hidden]="!mobileMenuOpen()">
  <!-- menu content -->
</nav>
```

---

### 3.2 Button Focus States

- **Location**: [product-card.html:68-86](sobee_Client/src/app/shared/components/product-card/product-card.html#L68-L86)
- **Issue**: Quantity buttons lack visible focus states for keyboard navigation.
- **Suggestion**:
```html
<!-- Add focus-visible ring: -->
<button
  (click)="decrementQuantity()"
  class="w-8 h-8 flex items-center justify-center text-slate-600
         hover:bg-slate-100
         focus-visible:ring-2 focus-visible:ring-primary-500 focus-visible:ring-offset-1
         disabled:opacity-50 disabled:cursor-not-allowed rounded-l-lg transition-colors"
>
```

---

### 3.3 Form Input Labels

- **Location**: [cart.html:164-169](sobee_Client/src/app/features/cart/cart.html#L164-L169)
- **Issue**: Promo code input has a label but no `for`/`id` association.
- **Suggestion**:
```html
<!-- Current: -->
<label class="text-sm font-medium text-slate-700">Promo code</label>
<input type="text" [(ngModel)]="promoCode" placeholder="Enter code" class="input py-2" />

<!-- Fixed: -->
<label for="promo-code" class="text-sm font-medium text-slate-700">Promo code</label>
<input
  id="promo-code"
  type="text"
  [(ngModel)]="promoCode"
  placeholder="Enter code"
  class="input py-2"
/>
```

---

### 3.4 Empty State Improvements

- **Location**: [cart.html:188-197](sobee_Client/src/app/features/cart/cart.html#L188-L197)
- **Assessment**: The empty cart state is well implemented with icon, heading, description, and CTA. No changes needed.

---

### 3.5 Skeleton Loaders (Good Pattern)

- **Location**: [home.html:108-121](sobee_Client/src/app/features/home/home.html#L108-L121), [cart.html:20-45](sobee_Client/src/app/features/cart/cart.html#L20-L45)
- **Assessment**: Skeleton loaders are properly implemented with `animate-pulse` class. This is a good UX pattern. No changes needed.

---

### 3.6 Hardcoded Pixel Values

- **Location**: [cart.html:21](sobee_Client/src/app/features/cart/cart.html#L21)
- **Issue**: Grid template uses hardcoded `360px` for sidebar width.
- **Current**:
```html
<div class="grid lg:grid-cols-[minmax(0,1fr)_360px] gap-8">
```
- **Suggestion**: Consider responsive sizing:
```html
<div class="grid lg:grid-cols-[minmax(0,1fr)_minmax(300px,360px)] gap-8">
```

---

### 3.7 Missing `loading="lazy"` on Images

- **Location**: [product-card.html:8-11](sobee_Client/src/app/shared/components/product-card/product-card.html#L8-L11)
- **Issue**: Product images should use native lazy loading for performance.
- **Suggestion**:
```html
<img
  [src]="productImage"
  [alt]="product.name ?? 'Sobee product'"
  loading="lazy"
  class="w-full h-full object-cover ..."
/>
```

---

## 4. Summary

### Critical (Fix Immediately)
| # | Issue | Location |
|---|-------|----------|
| 1.1 | N+1 query in Reviews | ReviewsController.cs:38-63 |
| 1.4 | Duplicate 422 error toasts | error.interceptor.ts:58-60 |
| 1.5 | Layout shift from images | product-card.html:8-11 |

### High Priority
| # | Issue | Location |
|---|-------|----------|
| 1.2 | Missing AsNoTracking | CartController.cs:346-351 |
| 1.6 | Cart images not displayed | cart.html:53-55 |
| 2.3 | Missing OnPush strategy | Multiple components |

### Medium Priority
| # | Issue | Location |
|---|-------|----------|
| 1.7 | No correlation ID logging | error.interceptor.ts:78 |
| 2.6 | API URL inconsistency | auth.service.ts:22-23 |
| 2.7 | Duplicate error helpers | Multiple controllers |

### Low Priority (Polish)
| # | Issue | Location |
|---|-------|----------|
| 2.1 | Primary constructors | All controllers |
| 2.2 | Collection expressions | ProductsController.cs:137 |
| 3.1-3.7 | A11y and UI polish | Various templates |

---

*Report generated: January 2026*

---

## 5. Codex Audit Addendum (Additional Findings)

### 5.1 Retry Policy Retries Non-Idempotent Requests

- **Location**: [error.interceptor.ts:20-29](sobee_Client/src/app/core/interceptors/error.interceptor.ts#L20-L29)
- **Issue**: Global retry applies to all HTTP methods. POST/PUT/PATCH/DELETE can be replayed on transient failures, risking duplicate orders/promos and inconsistent state.
- **Fix**:
```typescript
const SAFE_METHODS = new Set(['GET', 'HEAD', 'OPTIONS']);
const shouldRetry = SAFE_METHODS.has(req.method);

return next(req).pipe(
  retry({
    count: shouldRetry ? RETRY_COUNT : 0,
    delay: (error: HttpErrorResponse, retryCount: number) => {
      if (RETRYABLE_STATUS_CODES.includes(error.status)) {
        return timer(RETRY_DELAY * retryCount);
      }
      return throwError(() => error);
    }
  }),
  catchError(/* existing handler */)
);
```

---

### 5.2 Unbounded Orders Payload

- **Location**: [OrdersController.cs:69-84](sobee_API/sobee_API/Controllers/OrdersController.cs#L69-L84)
- **Issue**: `GetMyOrders` returns a full list with eager-loaded items. Large accounts will produce huge payloads and high memory usage.
- **Fix** (pagination):
```csharp
[HttpGet("my")]
[Authorize]
public async Task<IActionResult> GetMyOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
{
    if (page <= 0 || pageSize <= 0 || pageSize > 100)
        return BadRequestError("Invalid pagination.", "ValidationError");

    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (string.IsNullOrWhiteSpace(userId))
        return UnauthorizedError("Missing NameIdentifier claim.", "Unauthorized");

    var query = _db.Torders.AsNoTracking().Where(o => o.UserId == userId);
    var totalCount = await query.CountAsync();

    var orders = await query
        .OrderByDescending(o => o.DtmOrderDate)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Include(o => o.TorderItems)
        .ThenInclude(oi => oi.IntProduct)
        .ToListAsync();

    return Ok(new { page, pageSize, totalCount, items = orders.Select(ToOrderResponse) });
}
```

---

### 5.3 Admin Analytics Revenue Loads All Orders Into Memory

- **Location**: [AdminAnalyticsController.cs:39-58](sobee_API/sobee_API/Controllers/AdminAnalyticsController.cs#L39-L58)
- **Issue**: `GetRevenueByPeriod` materializes all records then groups in-memory. Large ranges will spike memory/CPU.
- **Fix** (server-side group):
```csharp
var grouped = await _db.Torders
    .AsNoTracking()
    .Where(o => o.DtmOrderDate != null && o.DtmOrderDate >= start && o.DtmOrderDate <= end)
    .GroupBy(o => o.DtmOrderDate!.Value.Date)
    .Select(g => new
    {
        date = g.Key,
        revenue = g.Sum(x => x.DecTotalAmount ?? 0m),
        orderCount = g.Count(),
        avgOrderValue = g.Count() == 0 ? 0m : g.Sum(x => x.DecTotalAmount ?? 0m) / g.Count()
    })
    .OrderBy(x => x.date)
    .ToListAsync();
```

---

### 5.4 Missing OnPush on Signal-Driven Components

- **Location**: [shop.ts:12-17](sobee_Client/src/app/features/shop/shop.ts#L12-L17), [home.ts:11-16](sobee_Client/src/app/features/home/home.ts#L11-L16), [admin-dashboard.ts:24-29](sobee_Client/src/app/features/admin/dashboard/admin-dashboard.ts#L24-L29), [cart.ts:10-15](sobee_Client/src/app/features/cart/cart.ts#L10-L15), [product-detail.ts:22-27](sobee_Client/src/app/features/product-detail/product-detail.ts#L22-L27)
- **Issue**: These components are signal-driven and should use `ChangeDetectionStrategy.OnPush` for better performance.
- **Fix**:
```typescript
import { ChangeDetectionStrategy } from '@angular/core';

@Component({
  // ...
  changeDetection: ChangeDetectionStrategy.OnPush
})
```

---

### 5.5 Route Subscription Cleanup (Memory Leaks)

- **Location**: [shop.ts:49](sobee_Client/src/app/features/shop/shop.ts#L49), [search.ts:32](sobee_Client/src/app/features/search/search.ts#L32), [product-detail.ts:61](sobee_Client/src/app/features/product-detail/product-detail.ts#L61)
- **Issue**: Subscriptions to `ActivatedRoute` are not cleaned up, which can leak on route reuse.
- **Fix**:
```typescript
import { DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

private readonly destroyRef = inject(DestroyRef);

this.route.queryParamMap
  .pipe(takeUntilDestroyed(this.destroyRef))
  .subscribe(params => { /* ... */ });
```

---

### 5.6 API Base URL Inconsistency (Auth Service)

- **Location**: [auth.service.ts:22-23](sobee_Client/src/app/core/services/auth.service.ts#L22-L23)
- **Issue**: Mixed usage of `environment.apiBaseUrl` and `environment.apiUrl` risks pointing to different hosts.
- **Fix**:
```typescript
private readonly apiUrl = environment.apiUrl;
private readonly meUrl = `${this.apiUrl}/me`;
```

---

## 6. Phase Plan to Resolve Findings

### Phase 1: Backend Performance + Query Safety
1. Fix ReviewsController N+1 issue (Include + projection).
2. Add pagination to `GetMyOrders` response.
3. Move analytics aggregation to server-side grouping.
4. Add `AsNoTracking()` to read-only queries where missing.

### Phase 2: Frontend Stability + Data Safety
1. Scope retry interceptor to idempotent requests only.
2. Add `takeUntilDestroyed` to route subscriptions.
3. Apply `ChangeDetectionStrategy.OnPush` to signal-driven components.
4. Standardize API base URL usage in auth service.

### Phase 3: UI/UX & Accessibility Polish
1. Add image dimensions + lazy loading to product cards.
2. Wire cart item images where available.
3. Add promo input label association and mobile menu ARIA attributes.
4. Add focus-visible states to quantity controls.
