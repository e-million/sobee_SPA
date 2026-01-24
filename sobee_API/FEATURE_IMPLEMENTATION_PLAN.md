# Sobee API - Feature Implementation Plan

**Created:** 2026-01-23
**Based On:** FEATURE_AUDIT_FINDINGS.txt
**Architecture:** Layered (Controller → Service → Repository) with Tests

---

## Executive Summary

This plan addresses the 10 identified gaps from the feature audit, organized into 3 phases based on dependencies and risk. Each feature follows the established layered architecture pattern and includes required tests.

### Gap Priority Matrix

| Priority | Gap ID | Description | Phase | Effort |
|----------|--------|-------------|-------|--------|
| **Critical** | CAT-003 | Categories API endpoint missing | 1 | S |
| **Critical** | AUTH-003 | Password reset flow missing | 1 | M |
| **Critical** | CAT-007 | Product active/inactive flag missing | 1 | S |
| **High** | ADMIN-002 | Category CRUD missing | 1 | M |
| **High** | ORD-002 | Billing address not on order | 2 | M |
| **High** | CART-006 | Tax calculation missing | 2 | M |
| **High** | TEST-001 | Admin/auth integration tests missing | 2 | M |
| **Medium** | ADMIN-006 | Soft delete not supported | 3 | M |
| **Medium** | CAT-001 | Pagination UI not wired | 3 | S |
| **Low** | Payment | Payment provider placeholder | 3 | L |

**Legend:** S = Small (1-2 days), M = Medium (2-4 days), L = Large (5+ days)

---

## Phase 1: Core Catalog & Auth Gaps

**Goal:** Fix critical frontend-breaking issues (categories, password reset, product visibility)

### Feature 1.1: Categories API Endpoint [CAT-003]

**Problem:** Frontend calls `GET /api/categories` but no endpoint exists.

#### 1.1.1 Database Layer (No Changes Needed)

Entity already exists: `Sobee.Domain/Entities/Products/TdrinkCategory.cs`
DbSet already exists: `SobeecoredbContext.TdrinkCategories`

#### 1.1.2 Repository Layer

**Create:** `Sobee.Domain/Repositories/ICategoryRepository.cs`
```csharp
public interface ICategoryRepository
{
    Task<IReadOnlyList<TdrinkCategory>> GetAllAsync(CancellationToken ct = default);
    Task<TdrinkCategory?> FindByIdAsync(int categoryId, bool track = false, CancellationToken ct = default);
    Task<TdrinkCategory?> FindByNameAsync(string name, bool track = false, CancellationToken ct = default);
    Task<bool> ExistsAsync(int categoryId, CancellationToken ct = default);
    Task<bool> NameExistsAsync(string name, int? excludeId = null, CancellationToken ct = default);
    Task AddAsync(TdrinkCategory category, CancellationToken ct = default);
    Task RemoveAsync(TdrinkCategory category, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
```

**Create:** `Sobee.Domain/Repositories/CategoryRepository.cs`
```csharp
public class CategoryRepository : ICategoryRepository
{
    private readonly SobeecoredbContext _db;

    public CategoryRepository(SobeecoredbContext db) => _db = db;

    public async Task<IReadOnlyList<TdrinkCategory>> GetAllAsync(CancellationToken ct = default)
        => await _db.TdrinkCategories.AsNoTracking().OrderBy(c => c.StrName).ToListAsync(ct);

    // ... implement other methods
}
```

#### 1.1.3 Service Layer

**Create:** `sobee_API/Services/Interfaces/ICategoryService.cs`
```csharp
public interface ICategoryService
{
    Task<ServiceResult<IReadOnlyList<CategoryDto>>> GetAllCategoriesAsync(CancellationToken ct = default);
    Task<ServiceResult<CategoryDto>> GetCategoryByIdAsync(int categoryId, CancellationToken ct = default);
    Task<ServiceResult<CategoryDto>> CreateCategoryAsync(CreateCategoryRequest request, CancellationToken ct = default);
    Task<ServiceResult<CategoryDto>> UpdateCategoryAsync(int categoryId, UpdateCategoryRequest request, CancellationToken ct = default);
    Task<ServiceResult> DeleteCategoryAsync(int categoryId, CancellationToken ct = default);
}
```

**Create:** `sobee_API/Services/CategoryService.cs`
```csharp
public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IProductRepository _productRepository;
    private readonly ILogger<CategoryService> _logger;

    // Implementation with business rules:
    // - Cannot delete category with products
    // - Category name must be unique
}
```

#### 1.1.4 DTO Layer

**Create:** `sobee_API/DTOs/Categories/CategoryDto.cs`
```csharp
public record CategoryDto(int CategoryId, string Name, string? Description, int ProductCount);
```

**Create:** `sobee_API/DTOs/Categories/CreateCategoryRequest.cs`
```csharp
public record CreateCategoryRequest(string Name, string? Description);
```

**Create:** `sobee_API/DTOs/Categories/UpdateCategoryRequest.cs`
```csharp
public record UpdateCategoryRequest(string Name, string? Description);
```

#### 1.1.5 Validation Layer

**Create:** `sobee_API/Validation/CreateCategoryRequestValidator.cs`
```csharp
public class CreateCategoryRequestValidator : AbstractValidator<CreateCategoryRequest>
{
    public CreateCategoryRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}
```

#### 1.1.6 Mapping Layer

**Create:** `sobee_API/Mapping/CategoryMapping.cs`
```csharp
public static class CategoryMapping
{
    public static CategoryDto ToDto(this TdrinkCategory entity, int productCount = 0)
        => new(entity.IntDrinkCategoryId, entity.StrName ?? "", entity.StrDescription, productCount);
}
```

#### 1.1.7 Controller Layer

**Create:** `sobee_API/Controllers/CategoriesController.cs`
```csharp
[ApiController]
[Route("api/categories")]
public class CategoriesController : ApiControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
        => _categoryService = categoryService;

    [HttpGet]
    public async Task<IActionResult> GetCategories(CancellationToken ct)
    {
        var result = await _categoryService.GetAllCategoriesAsync(ct);
        return FromServiceResult(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetCategory(int id, CancellationToken ct)
    {
        var result = await _categoryService.GetCategoryByIdAsync(id, ct);
        return FromServiceResult(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryRequest request, CancellationToken ct)
    {
        var result = await _categoryService.CreateCategoryAsync(request, ct);
        return FromServiceResult(result, StatusCodes.Status201Created);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateCategory(int id, [FromBody] UpdateCategoryRequest request, CancellationToken ct)
    {
        var result = await _categoryService.UpdateCategoryAsync(id, request, ct);
        return FromServiceResult(result);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteCategory(int id, CancellationToken ct)
    {
        var result = await _categoryService.DeleteCategoryAsync(id, ct);
        return FromServiceResult(result, StatusCodes.Status204NoContent);
    }
}
```

#### 1.1.8 DI Registration

**Modify:** `sobee_API/Program.cs`
```csharp
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
```

#### 1.1.9 Tests

**Create:** `sobee_API.Tests/Services/CategoryServiceTests.cs`
```
CategoryServiceTests:
  - GetAllCategoriesAsync_ReturnsAllCategories
  - GetCategoryByIdAsync_Exists_ReturnsCategory
  - GetCategoryByIdAsync_NotExists_ReturnsNotFound
  - CreateCategoryAsync_ValidData_CreatesCategory
  - CreateCategoryAsync_DuplicateName_ReturnsConflict
  - UpdateCategoryAsync_ValidData_UpdatesCategory
  - UpdateCategoryAsync_NotFound_ReturnsNotFound
  - UpdateCategoryAsync_DuplicateName_ReturnsConflict
  - DeleteCategoryAsync_NoProducts_DeletesCategory
  - DeleteCategoryAsync_HasProducts_ReturnsConflict
```

**Create:** `sobee_API.Tests/Integration/CategoriesEndpointTests.cs`
```
CategoriesEndpointTests:
  - GetCategories_Returns200WithList
  - GetCategory_Exists_Returns200
  - GetCategory_NotExists_Returns404
  - CreateCategory_Admin_Returns201
  - CreateCategory_NonAdmin_Returns403
  - CreateCategory_DuplicateName_Returns409
  - UpdateCategory_Admin_Returns200
  - DeleteCategory_Admin_Returns204
  - DeleteCategory_HasProducts_Returns409
```

#### 1.1.10 Acceptance Criteria

- [ ] `GET /api/categories` returns list of all categories
- [ ] `GET /api/categories/{id}` returns single category
- [ ] `POST /api/categories` creates category (Admin only)
- [ ] `PUT /api/categories/{id}` updates category (Admin only)
- [ ] `DELETE /api/categories/{id}` deletes category (Admin only, no products)
- [ ] Frontend category dropdown works
- [ ] All tests pass

---

### Feature 1.2: Password Reset Flow [AUTH-003]

**Problem:** Frontend has forgot-password/reset-password routes but backend endpoints don't exist.

#### 1.2.1 Service Layer

**Create:** `sobee_API/Services/Interfaces/IPasswordResetService.cs`
```csharp
public interface IPasswordResetService
{
    Task<ServiceResult> RequestPasswordResetAsync(ForgotPasswordRequest request, CancellationToken ct = default);
    Task<ServiceResult> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default);
}
```

**Create:** `sobee_API/Services/PasswordResetService.cs`
```csharp
public class PasswordResetService : IPasswordResetService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<PasswordResetService> _logger;
    // Optional: IEmailSender for sending reset emails

    public async Task<ServiceResult> RequestPasswordResetAsync(ForgotPasswordRequest request, CancellationToken ct)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            // Don't reveal that user doesn't exist (security)
            return ServiceResult.Ok();
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);

        // TODO: Send email with token/link
        // For now, log the token (dev only)
        _logger.LogInformation("Password reset token for {Email}: {Token}", request.Email, token);

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return ServiceResult.Fail("INVALID_TOKEN", "Invalid or expired reset token.");
        }

        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return ServiceResult.Fail("RESET_FAILED", errors);
        }

        return ServiceResult.Ok();
    }
}
```

#### 1.2.2 DTO Layer

**Create:** `sobee_API/DTOs/Auth/ForgotPasswordRequest.cs`
```csharp
public record ForgotPasswordRequest(string Email);
```

**Create:** `sobee_API/DTOs/Auth/ResetPasswordRequest.cs`
```csharp
public record ResetPasswordRequest(string Email, string Token, string NewPassword);
```

#### 1.2.3 Validation Layer

**Create:** `sobee_API/Validation/ForgotPasswordRequestValidator.cs`
```csharp
public class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}
```

**Create:** `sobee_API/Validation/ResetPasswordRequestValidator.cs`
```csharp
public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(6);
    }
}
```

#### 1.2.4 Controller Layer

**Modify:** `sobee_API/Controllers/AuthController.cs`
```csharp
[HttpPost("forgot-password")]
public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken ct)
{
    var result = await _passwordResetService.RequestPasswordResetAsync(request, ct);
    return FromServiceResult(result);
}

[HttpPost("reset-password")]
public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken ct)
{
    var result = await _passwordResetService.ResetPasswordAsync(request, ct);
    return FromServiceResult(result);
}
```

#### 1.2.5 DI Registration

**Modify:** `sobee_API/Program.cs`
```csharp
builder.Services.AddScoped<IPasswordResetService, PasswordResetService>();
```

#### 1.2.6 Tests

**Create:** `sobee_API.Tests/Services/PasswordResetServiceTests.cs`
```
PasswordResetServiceTests:
  - RequestPasswordResetAsync_ValidEmail_ReturnsOk
  - RequestPasswordResetAsync_UnknownEmail_ReturnsOkSilently (security)
  - ResetPasswordAsync_ValidToken_ResetsPassword
  - ResetPasswordAsync_InvalidToken_ReturnsError
  - ResetPasswordAsync_ExpiredToken_ReturnsError
  - ResetPasswordAsync_WeakPassword_ReturnsError
```

**Create:** `sobee_API.Tests/Integration/PasswordResetEndpointTests.cs`
```
PasswordResetEndpointTests:
  - ForgotPassword_ValidEmail_Returns200
  - ForgotPassword_InvalidEmail_Returns400
  - ResetPassword_ValidToken_Returns200
  - ResetPassword_InvalidToken_Returns400
```

#### 1.2.7 Acceptance Criteria

- [ ] `POST /api/auth/forgot-password` accepts email and returns 200 (regardless of user existence)
- [ ] `POST /api/auth/reset-password` resets password with valid token
- [ ] Invalid tokens return appropriate error
- [ ] Frontend forgot-password/reset-password pages work
- [ ] All tests pass

---

### Feature 1.3: Product Active/Inactive Flag [CAT-007]

**Problem:** No way to hide products without deleting them.

#### 1.3.1 Database Migration

**Create Migration:** Add `IsActive` column to `Tproduct`

```csharp
// Migration: AddIsActiveToProduct
migrationBuilder.AddColumn<bool>(
    name: "IsActive",
    table: "Tproducts",
    type: "bit",
    nullable: false,
    defaultValue: true);

migrationBuilder.CreateIndex(
    name: "IX_Tproducts_IsActive",
    table: "Tproducts",
    column: "IsActive");
```

#### 1.3.2 Entity Layer

**Modify:** `Sobee.Domain/Entities/Products/Tproduct.cs`
```csharp
public bool IsActive { get; set; } = true;
```

#### 1.3.3 Repository Layer

**Modify:** `Sobee.Domain/Repositories/IProductRepository.cs`
```csharp
// Add parameter to filter active products
Task<(IReadOnlyList<Tproduct> Items, int TotalCount)> SearchAsync(
    ProductSearchParams searchParams,
    bool includeInactive = false, // New parameter
    CancellationToken ct = default);
```

**Modify:** `Sobee.Domain/Repositories/ProductRepository.cs`
```csharp
public async Task<(IReadOnlyList<Tproduct> Items, int TotalCount)> SearchAsync(
    ProductSearchParams searchParams,
    bool includeInactive = false,
    CancellationToken ct = default)
{
    var query = _db.Tproducts.AsNoTracking();

    // Filter inactive unless explicitly included (admin view)
    if (!includeInactive)
    {
        query = query.Where(p => p.IsActive);
    }

    // ... rest of search logic
}
```

#### 1.3.4 Service Layer

**Modify:** `sobee_API/Services/ProductService.cs`
```csharp
public async Task<ServiceResult<PagedResult<ProductListDto>>> GetProductsAsync(
    ProductSearchParams searchParams,
    bool isAdmin, // Pass from controller based on user role
    CancellationToken ct = default)
{
    // Admin sees all products; customers see only active
    var includeInactive = isAdmin;
    var (items, totalCount) = await _productRepository.SearchAsync(searchParams, includeInactive, ct);
    // ...
}

public async Task<ServiceResult<ProductDto>> SetProductActiveStatusAsync(
    int productId,
    bool isActive,
    CancellationToken ct = default)
{
    var product = await _productRepository.FindByIdAsync(productId, track: true, ct: ct);
    if (product == null)
        return ServiceResult<ProductDto>.NotFound("Product not found.");

    product.IsActive = isActive;
    await _productRepository.SaveChangesAsync(ct);

    return ServiceResult<ProductDto>.Ok(product.ToDto());
}
```

#### 1.3.5 DTO Layer

**Modify:** `sobee_API/DTOs/Products/ProductListDto.cs`
```csharp
public record ProductListDto(
    int ProductId,
    string Name,
    decimal Price,
    int StockAmount,
    string? PrimaryImageUrl,
    int? CategoryId,
    string? CategoryName,
    bool IsActive // Add this
);
```

**Modify:** `sobee_API/DTOs/Products/UpdateProductRequest.cs`
```csharp
public record UpdateProductRequest(
    string Name,
    string? Description,
    decimal Price,
    decimal? Cost,
    int StockAmount,
    int? CategoryId,
    bool? IsActive // Add this (optional, so existing calls don't break)
);
```

#### 1.3.6 Controller Layer

**Modify:** `sobee_API/Controllers/ProductsController.cs`
```csharp
[HttpGet]
public async Task<IActionResult> GetProducts([FromQuery] ProductSearchParams searchParams, CancellationToken ct)
{
    var isAdmin = User.IsInRole("Admin");
    var result = await _productService.GetProductsAsync(searchParams, isAdmin, ct);
    return FromServiceResult(result);
}

[HttpPatch("{id:int}/active")]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> SetProductActive(int id, [FromBody] SetActiveRequest request, CancellationToken ct)
{
    var result = await _productService.SetProductActiveStatusAsync(id, request.IsActive, ct);
    return FromServiceResult(result);
}
```

#### 1.3.7 Tests

**Add to:** `sobee_API.Tests/Services/ProductServiceTests.cs`
```
ProductServiceTests (new tests):
  - GetProductsAsync_Customer_ExcludesInactiveProducts
  - GetProductsAsync_Admin_IncludesInactiveProducts
  - SetProductActiveStatusAsync_ValidProduct_UpdatesStatus
  - SetProductActiveStatusAsync_NotFound_ReturnsNotFound
```

**Add to:** `sobee_API.Tests/Integration/ProductsEndpointTests.cs`
```
ProductsEndpointTests (new tests):
  - GetProducts_AsCustomer_ExcludesInactive
  - GetProducts_AsAdmin_IncludesInactive
  - SetProductActive_Admin_Returns200
  - SetProductActive_NonAdmin_Returns403
```

#### 1.3.8 Acceptance Criteria

- [ ] Migration adds `IsActive` column with default `true`
- [ ] Customer product listing excludes inactive products
- [ ] Admin product listing includes all products
- [ ] Admin can toggle product active status
- [ ] Frontend admin product management shows active toggle
- [ ] All tests pass

---

### Feature 1.4: Admin Category CRUD [ADMIN-002]

**Status:** Covered by Feature 1.1 (CategoriesController includes admin CRUD endpoints)

---

### Phase 1 Summary

| Feature | Files to Create | Files to Modify | Tests to Add |
|---------|-----------------|-----------------|--------------|
| 1.1 Categories | 8 new files | Program.cs | 19 tests |
| 1.2 Password Reset | 5 new files | AuthController.cs, Program.cs | 10 tests |
| 1.3 Product Active | 0 new files + 1 migration | 6 files | 8 tests |

**Phase 1 Test Gate:**
- [ ] All existing Phase 0 tests pass
- [ ] All new Category tests pass (unit + integration)
- [ ] All new Password Reset tests pass (unit + integration)
- [ ] All new Product Active tests pass (unit + integration)
- [ ] Frontend category dropdown works
- [ ] Frontend forgot-password flow works (even if email not sent)

---

## Phase 2: Checkout & Tax Enhancements

**Goal:** Complete checkout data model and add tax calculation

### Feature 2.1: Billing Address on Orders [ORD-002]

**Problem:** Checkout only accepts shipping address; billing address not stored on order.

#### 2.1.1 Database Migration

**Create Migration:** Add billing address fields to `Torder`

```csharp
// Migration: AddBillingAddressToOrder
migrationBuilder.AddColumn<string>(
    name: "StrBillingAddress",
    table: "Torders",
    type: "nvarchar(500)",
    maxLength: 500,
    nullable: true);
```

#### 2.1.2 Entity Layer

**Modify:** `Sobee.Domain/Entities/Orders/Torder.cs`
```csharp
public string? StrBillingAddress { get; set; }
```

#### 2.1.3 DTO Layer

**Modify:** `sobee_API/DTOs/Orders/CheckoutRequest.cs`
```csharp
public record CheckoutRequest(
    string ShippingAddress,
    string? BillingAddress, // Add this (optional, defaults to shipping)
    int PaymentMethodId
);
```

**Modify:** `sobee_API/DTOs/Orders/OrderResponse.cs`
```csharp
public record OrderResponse(
    int OrderId,
    // ... existing fields
    string ShippingAddress,
    string? BillingAddress, // Add this
    // ... rest
);
```

#### 2.1.4 Service Layer

**Modify:** `sobee_API/Services/OrderService.cs` (CheckoutAsync)
```csharp
var order = new Torder
{
    // ... existing fields
    StrShippingAddress = request.ShippingAddress,
    StrBillingAddress = request.BillingAddress ?? request.ShippingAddress, // Default to shipping
    // ...
};
```

#### 2.1.5 Mapping Layer

**Modify:** `sobee_API/Mapping/OrderMapping.cs`
```csharp
public static OrderResponse ToResponse(this Torder order, /* ... */)
{
    return new OrderResponse(
        // ... existing
        order.StrShippingAddress ?? "",
        order.StrBillingAddress,
        // ...
    );
}
```

#### 2.1.6 Tests

**Add to:** `sobee_API.Tests/Services/OrderServiceTests.cs`
```
OrderServiceTests (new tests):
  - CheckoutAsync_WithBillingAddress_StoresOnOrder
  - CheckoutAsync_NoBillingAddress_DefaultsToShipping
```

**Add to:** `sobee_API.Tests/Integration/OrdersEndpointTests.cs`
```
OrdersEndpointTests (new tests):
  - Checkout_WithBillingAddress_Returns201WithBilling
  - GetOrder_IncludesBillingAddress
```

#### 2.1.7 Acceptance Criteria

- [ ] Migration adds billing address column
- [ ] Checkout accepts optional billing address
- [ ] Order response includes billing address
- [ ] All tests pass

---

### Feature 2.2: Tax Calculation [CART-006]

**Problem:** Cart and order totals don't include tax.

#### 2.2.1 Configuration

**Add to:** `appsettings.json`
```json
{
  "TaxSettings": {
    "DefaultTaxRate": 0.08,
    "TaxEnabled": true
  }
}
```

**Create:** `sobee_API/Configuration/TaxSettings.cs`
```csharp
public class TaxSettings
{
    public decimal DefaultTaxRate { get; set; } = 0.08m;
    public bool TaxEnabled { get; set; } = true;
}
```

#### 2.2.2 Domain Layer

**Create:** `sobee_API/Domain/TaxCalculator.cs`
```csharp
public static class TaxCalculator
{
    public static decimal CalculateTax(decimal subtotal, decimal taxRate)
    {
        if (subtotal <= 0 || taxRate <= 0)
            return 0m;

        return Math.Round(subtotal * taxRate, 2, MidpointRounding.AwayFromZero);
    }

    public static decimal CalculateTotalWithTax(decimal subtotal, decimal discount, decimal taxRate)
    {
        var taxableAmount = subtotal - discount;
        if (taxableAmount < 0) taxableAmount = 0;

        var tax = CalculateTax(taxableAmount, taxRate);
        return taxableAmount + tax;
    }
}
```

#### 2.2.3 DTO Layer

**Modify:** `sobee_API/DTOs/Cart/CartResponseDto.cs`
```csharp
public record CartResponseDto(
    int CartId,
    string Owner,
    List<CartItemResponseDto> Items,
    CartPromoDto? AppliedPromo,
    decimal Subtotal,
    decimal Discount,
    decimal Tax,        // Add this
    decimal TaxRate,    // Add this
    decimal Total
);
```

**Modify:** `sobee_API/DTOs/Orders/OrderResponse.cs`
```csharp
public record OrderResponse(
    // ... existing
    decimal Subtotal,
    decimal Discount,
    decimal Tax,        // Add this
    decimal Total
    // ...
);
```

#### 2.2.4 Database Migration

**Create Migration:** Add tax fields to `Torder`

```csharp
// Migration: AddTaxFieldsToOrder
migrationBuilder.AddColumn<decimal>(
    name: "DecTaxAmount",
    table: "Torders",
    type: "decimal(18,2)",
    nullable: true);

migrationBuilder.AddColumn<decimal>(
    name: "DecTaxRate",
    table: "Torders",
    type: "decimal(5,4)",
    nullable: true);
```

#### 2.2.5 Entity Layer

**Modify:** `Sobee.Domain/Entities/Orders/Torder.cs`
```csharp
public decimal? DecTaxAmount { get; set; }
public decimal? DecTaxRate { get; set; }
```

#### 2.2.6 Service Layer

**Modify:** `sobee_API/Services/CartService.cs`
```csharp
private readonly TaxSettings _taxSettings;

public CartService(/* ... */, IOptions<TaxSettings> taxSettings)
{
    _taxSettings = taxSettings.Value;
}

private async Task<CartResponseDto> ProjectCartAsync(TshoppingCart cart, /* ... */)
{
    var subtotal = CartCalculator.CalculateSubtotal(items);
    var discount = promo != null ? PromoCalculator.CalculateDiscount(subtotal, promo.DiscountPercentage) : 0m;

    var taxRate = _taxSettings.TaxEnabled ? _taxSettings.DefaultTaxRate : 0m;
    var taxableAmount = subtotal - discount;
    var tax = TaxCalculator.CalculateTax(taxableAmount, taxRate);
    var total = taxableAmount + tax;

    return new CartResponseDto(
        // ...
        Subtotal: subtotal,
        Discount: discount,
        Tax: tax,
        TaxRate: taxRate,
        Total: total
    );
}
```

**Modify:** `sobee_API/Services/OrderService.cs` (CheckoutAsync)
```csharp
private readonly TaxSettings _taxSettings;

// In CheckoutAsync:
var taxRate = _taxSettings.TaxEnabled ? _taxSettings.DefaultTaxRate : 0m;
var taxableAmount = subtotal - discount;
var tax = TaxCalculator.CalculateTax(taxableAmount, taxRate);
var total = taxableAmount + tax;

var order = new Torder
{
    // ...
    DecSubtotal = subtotal,
    DecDiscountAmount = discount,
    DecTaxRate = taxRate,
    DecTaxAmount = tax,
    DecTotalAmount = total,
    // ...
};
```

#### 2.2.7 DI Registration

**Modify:** `sobee_API/Program.cs`
```csharp
builder.Services.Configure<TaxSettings>(builder.Configuration.GetSection("TaxSettings"));
```

#### 2.2.8 Tests

**Create:** `sobee_API.Tests/Domain/TaxCalculatorTests.cs`
```
TaxCalculatorTests:
  - CalculateTax_PositiveSubtotal_ReturnsCorrectTax
  - CalculateTax_ZeroSubtotal_ReturnsZero
  - CalculateTax_ZeroRate_ReturnsZero
  - CalculateTax_NegativeSubtotal_ReturnsZero
  - CalculateTotalWithTax_WithDiscount_CalculatesCorrectly
  - CalculateTotalWithTax_DiscountExceedsSubtotal_ReturnsZeroTax
```

**Add to:** `sobee_API.Tests/Services/CartServiceTests.cs`
```
CartServiceTests (new tests):
  - GetCartAsync_TaxEnabled_IncludesTaxInResponse
  - GetCartAsync_TaxDisabled_ReturnsZeroTax
```

**Add to:** `sobee_API.Tests/Services/OrderServiceTests.cs`
```
OrderServiceTests (new tests):
  - CheckoutAsync_TaxEnabled_StoresTaxOnOrder
  - CheckoutAsync_TaxDisabled_StoresZeroTax
```

**Add to:** `sobee_API.Tests/Integration/CartEndpointTests.cs`
```
CartEndpointTests (new tests):
  - GetCart_IncludesTaxFields
```

#### 2.2.9 Acceptance Criteria

- [ ] TaxCalculator has full unit test coverage
- [ ] Cart response includes tax and taxRate fields
- [ ] Order stores tax amount and rate
- [ ] Tax is calculated after discounts
- [ ] Tax can be disabled via configuration
- [ ] All tests pass

---

### Feature 2.3: Admin/Auth Integration Tests [TEST-001]

**Problem:** Admin endpoints and auth 401/403 scenarios lack integration test coverage.

#### 2.3.1 Tests to Add

**Create:** `sobee_API.Tests/Integration/AdminAuthorizationTests.cs`
```csharp
public class AdminAuthorizationTests : IClassFixture<SobeeWebApplicationFactory>
{
    // Test 401 scenarios (no token)
    [Theory]
    [InlineData("GET", "/api/admin/dashboard")]
    [InlineData("GET", "/api/admin/analytics/revenue")]
    [InlineData("GET", "/api/admin/users")]
    [InlineData("GET", "/api/admin/promos")]
    [InlineData("POST", "/api/products")]
    [InlineData("PATCH", "/api/orders/1/status")]
    public async Task AdminEndpoint_NoToken_Returns401(string method, string path)
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = method switch
        {
            "GET" => await client.GetAsync(path),
            "POST" => await client.PostAsync(path, new StringContent("{}", Encoding.UTF8, "application/json")),
            "PATCH" => await client.PatchAsync(path, new StringContent("{}", Encoding.UTF8, "application/json")),
            _ => throw new NotSupportedException()
        };

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Test 403 scenarios (user token, not admin)
    [Theory]
    [InlineData("GET", "/api/admin/dashboard")]
    [InlineData("GET", "/api/admin/analytics/revenue")]
    [InlineData("GET", "/api/admin/users")]
    [InlineData("POST", "/api/products")]
    public async Task AdminEndpoint_UserToken_Returns403(string method, string path)
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(role: "User");

        // Act
        var response = method switch
        {
            "GET" => await client.GetAsync(path),
            "POST" => await client.PostAsync(path, new StringContent("{}", Encoding.UTF8, "application/json")),
            _ => throw new NotSupportedException()
        };

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // Test 200 scenarios (admin token)
    [Fact]
    public async Task AdminDashboard_AdminToken_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient(role: "Admin");
        var response = await client.GetAsync("/api/admin/dashboard");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
```

**Create:** `sobee_API.Tests/Integration/AdminAnalyticsEndpointTests.cs`
```
AdminAnalyticsEndpointTests:
  - GetRevenue_Admin_Returns200
  - GetRevenue_NonAdmin_Returns403
  - GetOrderStatus_Admin_Returns200
  - GetRatingDistribution_Admin_Returns200
  - GetRecentReviews_Admin_Returns200
  - GetWorstProducts_Admin_Returns200
  - GetCategoryPerformance_Admin_Returns200
  - GetInventorySummary_Admin_Returns200
  - GetFulfillmentMetrics_Admin_Returns200
  - GetCustomerBreakdown_Admin_Returns200
  - GetCustomerGrowth_Admin_Returns200
  - GetTopCustomers_Admin_Returns200
  - GetMostWishlisted_Admin_Returns200
```

**Create:** `sobee_API.Tests/Integration/AdminUsersEndpointTests.cs`
```
AdminUsersEndpointTests:
  - GetUsers_Admin_Returns200
  - GetUsers_NonAdmin_Returns403
  - UpdateUserRole_Admin_Returns200
  - UpdateUserRole_NonAdmin_Returns403
```

**Create:** `sobee_API.Tests/Integration/AdminPromosEndpointTests.cs`
```
AdminPromosEndpointTests:
  - GetPromos_Admin_Returns200
  - CreatePromo_Admin_Returns201
  - UpdatePromo_Admin_Returns200
  - DeletePromo_Admin_Returns204
  - AllEndpoints_NonAdmin_Returns403
```

#### 2.3.2 Test Infrastructure Update

**Modify:** `sobee_API.Tests/Infrastructure/SobeeWebApplicationFactory.cs`
```csharp
public HttpClient CreateAuthenticatedClient(string role = "User", string? userId = null)
{
    var client = CreateClient();

    // Create test user and get token
    var token = GenerateTestToken(role, userId ?? Guid.NewGuid().ToString());
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    return client;
}
```

#### 2.3.3 Acceptance Criteria

- [ ] All admin endpoints return 401 without token
- [ ] All admin endpoints return 403 with non-admin token
- [ ] All admin endpoints return 200/201/204 with admin token
- [ ] Test coverage includes all analytics endpoints
- [ ] All tests pass and are not flaky

---

### Phase 2 Summary

| Feature | Files to Create | Files to Modify | Migrations | Tests to Add |
|---------|-----------------|-----------------|------------|--------------|
| 2.1 Billing Address | 0 | 5 files | 1 | 4 tests |
| 2.2 Tax Calculation | 2 new files | 6 files | 1 | 12 tests |
| 2.3 Admin Tests | 4 new test files | 1 file | 0 | 30+ tests |

**Phase 2 Test Gate:**
- [ ] All Phase 1 tests still pass
- [ ] All billing address tests pass
- [ ] TaxCalculator has 100% coverage
- [ ] All tax integration tests pass
- [ ] All admin authorization tests pass (401/403)
- [ ] All admin endpoint tests pass

---

## Phase 3: Quality & Polish

**Goal:** Soft delete, pagination UX, payment foundation

### Feature 3.1: Soft Delete for Products [ADMIN-006]

**Problem:** Deleting products is permanent; no archival mechanism.

#### 3.1.1 Database Migration

```csharp
// Migration: AddSoftDeleteToProduct
migrationBuilder.AddColumn<bool>(
    name: "IsDeleted",
    table: "Tproducts",
    type: "bit",
    nullable: false,
    defaultValue: false);

migrationBuilder.AddColumn<DateTime>(
    name: "DeletedAtUtc",
    table: "Tproducts",
    type: "datetime2",
    nullable: true);

migrationBuilder.CreateIndex(
    name: "IX_Tproducts_IsDeleted",
    table: "Tproducts",
    column: "IsDeleted");
```

#### 3.1.2 Entity Layer

**Modify:** `Sobee.Domain/Entities/Products/Tproduct.cs`
```csharp
public bool IsDeleted { get; set; } = false;
public DateTime? DeletedAtUtc { get; set; }
```

#### 3.1.3 Repository Layer

**Modify:** `Sobee.Domain/Repositories/ProductRepository.cs`
```csharp
// Add global query filter or filter in all queries
public async Task<(IReadOnlyList<Tproduct> Items, int TotalCount)> SearchAsync(
    ProductSearchParams searchParams,
    bool includeInactive = false,
    bool includeDeleted = false, // New parameter
    CancellationToken ct = default)
{
    var query = _db.Tproducts.AsNoTracking();

    if (!includeDeleted)
    {
        query = query.Where(p => !p.IsDeleted);
    }

    // ... rest
}

public async Task SoftDeleteAsync(Tproduct product, CancellationToken ct = default)
{
    product.IsDeleted = true;
    product.DeletedAtUtc = DateTime.UtcNow;
    await SaveChangesAsync(ct);
}

public async Task RestoreAsync(Tproduct product, CancellationToken ct = default)
{
    product.IsDeleted = false;
    product.DeletedAtUtc = null;
    await SaveChangesAsync(ct);
}
```

#### 3.1.4 Service Layer

**Modify:** `sobee_API/Services/ProductService.cs`
```csharp
public async Task<ServiceResult> DeleteProductAsync(int productId, bool hardDelete = false, CancellationToken ct = default)
{
    var product = await _productRepository.FindByIdAsync(productId, track: true, ct: ct);
    if (product == null)
        return ServiceResult.NotFound("Product not found.");

    if (hardDelete)
    {
        await _productRepository.RemoveAsync(product, ct);
    }
    else
    {
        await _productRepository.SoftDeleteAsync(product, ct);
    }

    return ServiceResult.Ok();
}

public async Task<ServiceResult<ProductDto>> RestoreProductAsync(int productId, CancellationToken ct = default)
{
    var product = await _productRepository.FindByIdAsync(productId, track: true, includeDeleted: true, ct: ct);
    if (product == null)
        return ServiceResult<ProductDto>.NotFound("Product not found.");

    if (!product.IsDeleted)
        return ServiceResult<ProductDto>.Fail("ALREADY_ACTIVE", "Product is not deleted.");

    await _productRepository.RestoreAsync(product, ct);
    return ServiceResult<ProductDto>.Ok(product.ToDto());
}
```

#### 3.1.5 Controller Layer

**Modify:** `sobee_API/Controllers/ProductsController.cs`
```csharp
[HttpDelete("{id:int}")]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> DeleteProduct(int id, [FromQuery] bool hardDelete = false, CancellationToken ct)
{
    var result = await _productService.DeleteProductAsync(id, hardDelete, ct);
    return FromServiceResult(result, StatusCodes.Status204NoContent);
}

[HttpPost("{id:int}/restore")]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> RestoreProduct(int id, CancellationToken ct)
{
    var result = await _productService.RestoreProductAsync(id, ct);
    return FromServiceResult(result);
}
```

#### 3.1.6 Tests

```
ProductServiceTests (new tests):
  - DeleteProductAsync_SoftDelete_SetsIsDeletedFlag
  - DeleteProductAsync_HardDelete_RemovesFromDb
  - RestoreProductAsync_DeletedProduct_RestoresProduct
  - RestoreProductAsync_NotDeleted_ReturnsError
  - GetProductsAsync_ExcludesDeletedByDefault

ProductsEndpointTests (new tests):
  - DeleteProduct_SoftDelete_Returns204AndHidesProduct
  - DeleteProduct_HardDelete_Returns204AndRemovesProduct
  - RestoreProduct_Admin_Returns200
  - GetProducts_ExcludesSoftDeleted
```

#### 3.1.7 Acceptance Criteria

- [ ] Products can be soft-deleted (hidden but recoverable)
- [ ] Soft-deleted products excluded from customer queries
- [ ] Admin can view deleted products with query parameter
- [ ] Admin can restore soft-deleted products
- [ ] Hard delete available for permanent removal
- [ ] All tests pass

---

### Feature 3.2: Pagination UI Support [CAT-001]

**Problem:** Frontend doesn't display pagination controls; backend pagination exists.

#### 3.2.1 DTO Enhancement

**Create:** `sobee_API/DTOs/Common/PagedResponse.cs`
```csharp
public record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages
)
{
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}
```

#### 3.2.2 Service Layer

**Modify:** `sobee_API/Services/ProductService.cs`
```csharp
public async Task<ServiceResult<PagedResponse<ProductListDto>>> GetProductsAsync(
    ProductSearchParams searchParams,
    bool isAdmin,
    CancellationToken ct = default)
{
    var (items, totalCount) = await _productRepository.SearchAsync(searchParams, isAdmin, ct);

    var page = searchParams.Page ?? 1;
    var pageSize = searchParams.PageSize ?? 20;
    var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

    var dtos = items.Select(p => p.ToListDto()).ToList();

    return ServiceResult<PagedResponse<ProductListDto>>.Ok(
        new PagedResponse<ProductListDto>(dtos, page, pageSize, totalCount, totalPages)
    );
}
```

#### 3.2.3 Controller Layer (Already done, verify)

Verify `ProductsController.GetProducts` returns pagination metadata in response body (not just headers).

#### 3.2.4 Tests

```
ProductsEndpointTests (new tests):
  - GetProducts_ReturnsPaginationMetadata
  - GetProducts_Page2_ReturnsCorrectItems
  - GetProducts_LastPage_HasNextPageFalse
```

#### 3.2.5 Acceptance Criteria

- [ ] Product list response includes pagination metadata
- [ ] Page, pageSize, totalCount, totalPages in response
- [ ] HasPreviousPage/HasNextPage booleans for UI
- [ ] Frontend can render pagination controls
- [ ] All tests pass

---

### Feature 3.3: Payment Provider Foundation (Optional) [Payment]

**Problem:** PayOrder sets status but doesn't integrate with payment provider.

#### 3.3.1 Interface for Payment Provider

**Create:** `sobee_API/Services/Interfaces/IPaymentProvider.cs`
```csharp
public interface IPaymentProvider
{
    Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request, CancellationToken ct = default);
    Task<PaymentResult> RefundPaymentAsync(string transactionId, decimal amount, CancellationToken ct = default);
}

public record PaymentRequest(
    decimal Amount,
    string Currency,
    string CardNumber,
    string CardExpiry,
    string CardCvv,
    string BillingAddress
);

public record PaymentResult(
    bool Success,
    string? TransactionId,
    string? ErrorCode,
    string? ErrorMessage
);
```

#### 3.3.2 Mock Implementation

**Create:** `sobee_API/Services/MockPaymentProvider.cs`
```csharp
public class MockPaymentProvider : IPaymentProvider
{
    private readonly ILogger<MockPaymentProvider> _logger;

    public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request, CancellationToken ct = default)
    {
        // Simulate processing delay
        await Task.Delay(100, ct);

        // Mock: cards starting with 4 succeed, others fail
        if (request.CardNumber.StartsWith("4"))
        {
            var transactionId = $"TXN_{Guid.NewGuid():N}";
            _logger.LogInformation("Mock payment succeeded: {TransactionId}", transactionId);
            return new PaymentResult(true, transactionId, null, null);
        }

        _logger.LogWarning("Mock payment failed for card: {CardPrefix}****", request.CardNumber[..4]);
        return new PaymentResult(false, null, "CARD_DECLINED", "Card was declined.");
    }

    public async Task<PaymentResult> RefundPaymentAsync(string transactionId, decimal amount, CancellationToken ct = default)
    {
        await Task.Delay(50, ct);
        _logger.LogInformation("Mock refund processed: {TransactionId}, Amount: {Amount}", transactionId, amount);
        return new PaymentResult(true, $"REF_{transactionId}", null, null);
    }
}
```

#### 3.3.3 DI Registration

**Modify:** `sobee_API/Program.cs`
```csharp
// Use mock in development, real provider in production
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddScoped<IPaymentProvider, MockPaymentProvider>();
}
else
{
    // builder.Services.AddScoped<IPaymentProvider, StripePaymentProvider>();
    builder.Services.AddScoped<IPaymentProvider, MockPaymentProvider>(); // Until real provider added
}
```

#### 3.3.4 Integration with OrderService

**Modify:** `sobee_API/Services/OrderService.cs` (PayOrderAsync)
```csharp
public async Task<ServiceResult<OrderDto>> PayOrderAsync(
    string? userId,
    string? sessionId,
    int orderId,
    PayOrderRequest request,
    CancellationToken ct = default)
{
    // ... existing validation

    // Process payment
    var paymentRequest = new PaymentRequest(
        Amount: order.DecTotalAmount ?? 0,
        Currency: "USD",
        CardNumber: request.CardNumber,
        CardExpiry: request.CardExpiry,
        CardCvv: request.CardCvv,
        BillingAddress: order.StrBillingAddress ?? order.StrShippingAddress ?? ""
    );

    var paymentResult = await _paymentProvider.ProcessPaymentAsync(paymentRequest, ct);

    if (!paymentResult.Success)
    {
        return ServiceResult<OrderDto>.Fail(
            paymentResult.ErrorCode ?? "PAYMENT_FAILED",
            paymentResult.ErrorMessage ?? "Payment processing failed."
        );
    }

    // Store transaction ID
    order.StrPaymentTransactionId = paymentResult.TransactionId;
    order.StrOrderStatus = OrderStatuses.Paid;
    order.DtmPaidDate = DateTime.UtcNow;

    // ... rest
}
```

#### 3.3.5 Tests

```
MockPaymentProviderTests:
  - ProcessPaymentAsync_ValidCard_ReturnsSuccess
  - ProcessPaymentAsync_InvalidCard_ReturnsFailure
  - RefundPaymentAsync_ReturnsSuccess

OrderServiceTests (payment tests):
  - PayOrderAsync_PaymentSucceeds_UpdatesOrderStatus
  - PayOrderAsync_PaymentFails_ReturnsErrorAndNoStatusChange
```

#### 3.3.6 Acceptance Criteria

- [ ] IPaymentProvider interface defined
- [ ] MockPaymentProvider works for testing
- [ ] PayOrder integrates with payment provider
- [ ] Payment failures return proper error codes
- [ ] Transaction ID stored on order
- [ ] All tests pass

---

### Phase 3 Summary

| Feature | Files to Create | Files to Modify | Migrations | Tests to Add |
|---------|-----------------|-----------------|------------|--------------|
| 3.1 Soft Delete | 0 | 4 files | 1 | 9 tests |
| 3.2 Pagination | 1 new file | 2 files | 0 | 3 tests |
| 3.3 Payment | 3 new files | 2 files | 0 | 5 tests |

**Phase 3 Test Gate:**
- [ ] All Phase 1 and 2 tests still pass
- [ ] Soft delete tests pass
- [ ] Pagination metadata tests pass
- [ ] Payment provider tests pass
- [ ] Full regression with 0 failures

---

## Implementation Checklist

### Phase 1 Checklist

- [ ] **1.1 Categories API**
  - [ ] Create ICategoryRepository + CategoryRepository
  - [ ] Create ICategoryService + CategoryService
  - [ ] Create CategoryDto + CreateCategoryRequest + UpdateCategoryRequest
  - [ ] Create validators
  - [ ] Create CategoriesController
  - [ ] Register in DI
  - [ ] Write unit tests (CategoryServiceTests)
  - [ ] Write integration tests (CategoriesEndpointTests)
  - [ ] Verify frontend dropdown works

- [ ] **1.2 Password Reset**
  - [ ] Create IPasswordResetService + PasswordResetService
  - [ ] Create ForgotPasswordRequest + ResetPasswordRequest
  - [ ] Create validators
  - [ ] Add endpoints to AuthController
  - [ ] Register in DI
  - [ ] Write unit tests
  - [ ] Write integration tests
  - [ ] Verify frontend flow works

- [ ] **1.3 Product Active Flag**
  - [ ] Create migration (AddIsActiveToProduct)
  - [ ] Modify Tproduct entity
  - [ ] Modify ProductRepository (filter logic)
  - [ ] Modify ProductService (admin vs customer)
  - [ ] Add endpoint for toggling active status
  - [ ] Modify DTOs
  - [ ] Write tests
  - [ ] Verify admin UI shows toggle

### Phase 2 Checklist

- [ ] **2.1 Billing Address**
  - [ ] Create migration (AddBillingAddressToOrder)
  - [ ] Modify Torder entity
  - [ ] Modify CheckoutRequest DTO
  - [ ] Modify OrderResponse DTO
  - [ ] Modify OrderService.CheckoutAsync
  - [ ] Modify OrderMapping
  - [ ] Write tests

- [ ] **2.2 Tax Calculation**
  - [ ] Create TaxSettings configuration
  - [ ] Create TaxCalculator domain class
  - [ ] Create migration (AddTaxFieldsToOrder)
  - [ ] Modify Torder entity
  - [ ] Modify CartResponseDto
  - [ ] Modify CartService
  - [ ] Modify OrderService
  - [ ] Register TaxSettings in DI
  - [ ] Write TaxCalculatorTests
  - [ ] Write service tests
  - [ ] Write integration tests

- [ ] **2.3 Admin Tests**
  - [ ] Create AdminAuthorizationTests
  - [ ] Create AdminAnalyticsEndpointTests
  - [ ] Create AdminUsersEndpointTests
  - [ ] Create AdminPromosEndpointTests
  - [ ] Update test infrastructure for role-based auth
  - [ ] Run full test suite

### Phase 3 Checklist

- [ ] **3.1 Soft Delete**
  - [ ] Create migration (AddSoftDeleteToProduct)
  - [ ] Modify Tproduct entity
  - [ ] Modify ProductRepository
  - [ ] Modify ProductService
  - [ ] Add restore endpoint
  - [ ] Write tests

- [ ] **3.2 Pagination**
  - [ ] Create PagedResponse<T> DTO
  - [ ] Modify ProductService response
  - [ ] Verify controller returns metadata
  - [ ] Write tests

- [ ] **3.3 Payment Provider**
  - [ ] Create IPaymentProvider interface
  - [ ] Create MockPaymentProvider
  - [ ] Modify OrderService.PayOrderAsync
  - [ ] Register in DI
  - [ ] Write tests

---

## Traceability Matrix

| Gap ID | Description | Phase | Feature | Verification Tests |
|--------|-------------|-------|---------|-------------------|
| CAT-003 | Categories endpoint | 1 | 1.1 | CategoriesEndpointTests |
| AUTH-003 | Password reset | 1 | 1.2 | PasswordResetEndpointTests |
| CAT-007 | Product active flag | 1 | 1.3 | ProductServiceTests, ProductsEndpointTests |
| ADMIN-002 | Category CRUD | 1 | 1.1 | CategoriesEndpointTests (admin) |
| ORD-002 | Billing address | 2 | 2.1 | OrdersEndpointTests |
| CART-006 | Tax calculation | 2 | 2.2 | TaxCalculatorTests, CartEndpointTests |
| TEST-001 | Admin tests | 2 | 2.3 | AdminAuthorizationTests, Admin*Tests |
| ADMIN-006 | Soft delete | 3 | 3.1 | ProductServiceTests, ProductsEndpointTests |
| CAT-001 | Pagination UI | 3 | 3.2 | ProductsEndpointTests |
| Payment | Payment provider | 3 | 3.3 | MockPaymentProviderTests, OrderServiceTests |

---

## Definition of Done (Per Phase)

### Phase 1 Complete When:
- [ ] All new repositories have interface + implementation
- [ ] All new services have interface + implementation
- [ ] All new endpoints respond correctly
- [ ] All unit tests pass
- [ ] All integration tests pass
- [ ] Frontend category dropdown works
- [ ] Frontend password reset flow works
- [ ] Admin can toggle product visibility

### Phase 2 Complete When:
- [ ] Tax displayed in cart and stored on orders
- [ ] Billing address stored on orders
- [ ] 100% admin endpoint test coverage (401/403/200)
- [ ] All existing tests still pass
- [ ] No regressions in checkout flow

### Phase 3 Complete When:
- [ ] Soft delete works for products
- [ ] Pagination metadata in product responses
- [ ] Payment provider abstraction in place
- [ ] Full test suite passes 10x consecutively
- [ ] Zero flaky tests

---

**Document Created:** 2026-01-23
**Based On:** FEATURE_AUDIT_FINDINGS.txt
