# Sobee Angular SPA - Complete Implementation Plan

> **Purpose**: This document serves as a comprehensive guide for continuing development of the Sobee e-commerce Angular application. It is designed to be used as context for an LLM assistant or developer to understand the current state, remaining work, and implementation details.

---

## Table of Contents
1. [Project Overview](#project-overview)
2. [Architecture Decisions & Assumptions](#architecture-decisions--assumptions)
3. [Current Architecture](#current-architecture)
4. [State & Data Ownership](#state--data-ownership)
5. [App Initialization & Session Management](#app-initialization--session-management)
6. [Error Handling Specification](#error-handling-specification)
7. [API Contracts](#api-contracts)
8. [Completed Features](#completed-features)
9. [Remaining Implementation Tasks](#remaining-implementation-tasks)
10. [Phase Checklists](#phase-checklists)
11. [Detailed Implementation Specifications](#detailed-implementation-specifications)
12. [Angular Code Constraints](#angular-code-constraints)
13. [Styling & Design System](#styling--design-system)
14. [Testing Strategy](#testing-strategy)
15. [Deployment Considerations](#deployment-considerations)
16. [Open Questions & Decisions Log](#open-questions--decisions-log)

---

## Project Overview

### What is Sobee?
Sobee is an e-commerce platform for a non-alcoholic aperitif/energy drink company. The project is a modernization effort, migrating from an ASP.NET MVC monolith to a decoupled architecture:
- **Backend**: ASP.NET Core Web API (located in `sobee_Core/`)
- **Frontend**: Angular 20 SPA with Tailwind CSS (located in `sobee_Client/`)

### Repository Structure
```
sobee_SPA/
|-- sobee_Client/          # Angular frontend application
|   |-- src/
|   |   |-- app/
|   |   |   |-- core/       # Services, models, guards, interceptors
|   |   |   |-- features/   # Page components (home, shop, checkout, etc.)
|   |   |   `-- shared/     # Reusable components (navbar, footer, product-card)
|   |-- environments/       # Environment configuration
|   |-- styles.css          # Global Tailwind styles
|   |-- tailwind.config.js
|   `-- package.json
|-- sobee_Core/             # ASP.NET Core API
|-- FEATURE_ROADMAP.md      # Original feature analysis
`-- IMPLEMENTATION_PLAN.md  # This document
```

### Technology Stack
- **Angular 20** with standalone components
- **TypeScript** for type safety
- **Tailwind CSS v3** for styling
- **Angular Signals** for reactive state management
- **RxJS** for HTTP operations
- **JWT Authentication** with refresh token support

### Running the Application
```bash
# Frontend (from sobee_Client/)
npm install
npm start        # Runs on http://localhost:4200

# Backend (from sobee_Core/)
dotnet run       # Runs on https://localhost:7058
```

---

## Architecture Decisions & Assumptions

### Token Storage Strategy

**Current Implementation** (in `auth.service.ts`):
| Item | Storage Key | Location |
|------|-------------|----------|
| Access Token | `accessToken` | localStorage |
| Refresh Token | `refreshToken` | localStorage |
| Guest Session ID | `guestSessionId` | localStorage |
| Guest Session Secret | `guestSessionSecret` | localStorage |

| Decision | Value | Rationale |
|----------|-------|-----------|
| **Token Storage** | `localStorage` | Simple, works across tabs, acceptable for this app's threat model |
| **Token Rotation** | Not required | API issues new tokens on refresh |
| **Multi-tab Sync** | Via `storage` event on `accessToken` key | Listen for login/logout in other tabs |
| **Logout Behavior** | Clear auth tokens + guest session, redirect to `/` | Full session cleanup |
| **withCredentials** | `false` | Using Bearer tokens, not cookies |

**Security Note**: For higher-security requirements, consider:
- HttpOnly cookies for refresh tokens (requires API changes)
- In-memory access tokens with silent refresh
- PKCE flow for additional protection

### Guest Session Behavior

**Current Implementation**: Guest headers are ALWAYS attached if present in localStorage. Auth interceptor runs after and adds Bearer token. API uses auth token when present, falls back to guest session.

| Scenario | Current Behavior | Notes |
|----------|------------------|-------|
| **Guest adds to cart** | Creates guest session via response headers | Interceptor captures X-Session-Id/X-Session-Secret |
| **Guest session + authenticated** | Both headers sent; API prioritizes auth | No suppression currently |
| **Login with guest cart** | API merges guest cart into user cart | Merge happens server-side |
| **After successful login** | Guest session NOT auto-cleared | Must call `clearGuestSession()` manually |
| **Logout** | Auth tokens cleared; guest session NOT cleared | Need to update `logout()` to clear guest |

**TODO**: Update `logout()` in `auth.service.ts` to also call `clearGuestSession()`.

### Guest-to-Auth Cart Merge Rules

| Scenario | Behavior |
|----------|----------|
| Same product in both carts | Server decides (typically: sum quantities, cap at max) |
| Guest promo code | Retained if valid for user |
| Guest cart empty | No merge needed |
| Auth cart empty | Guest cart becomes user cart |
| Merge conflict | Server-authoritative; client refreshes cart after login |

### Product IDs
- **Type**: `number` (integer)
- **Route format**: `/product/:id` accepts numeric IDs only
- **Validation**: Parse with `+params['id']`, show 404 if NaN or not found

### Cart Totals Source of Truth
- **Server-authoritative**: All totals (subtotal, discount, tax, shipping, total) come from API
- **Client displays only**: Frontend never calculates totals
- **Tax/Shipping**: Calculated server-side during checkout (not shown in cart preview)
- **Cart UI**: Server-confirmed (not optimistic) - wait for API response before updating UI

### Currency Formatting

- **Currency**: USD only
- **Formatting**: Use Angular `CurrencyPipe` or `Intl.NumberFormat`, NOT `toFixed(2)`
- **Locale**: `en-US`

**In templates**:
```html
{{ product.price | currency:'USD':'symbol':'1.2-2' }}
<!-- Output: $19.99 -->
```

**In TypeScript** (if needed):
```typescript
formatCurrency(value: number): string {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
    minimumFractionDigits: 2,
    maximumFractionDigits: 2
  }).format(value);
}
```

**Rationale**: `toFixed(2)` has rounding issues and doesn't handle locale-specific formatting. `CurrencyPipe` and `Intl.NumberFormat` handle edge cases correctly.

### Quantity Constraints
- **Min**: 1 (enforced client-side)
- **Max**: `Math.min(10, product.stockAmount)` - respects stock
- **Out of stock**: Disable add-to-cart button, show "Out of Stock" badge
- **Low stock**: Show "Only X left" when `stockAmount < 5`

### SSR / i18n / Currency
- **SSR**: Explicitly NOT supported for now (client-side only)
- **i18n**: English only; no translation infrastructure
- **Currency**: USD only; format using `CurrencyPipe` or `Intl.NumberFormat`

---

## Current Architecture

### Core Services (`src/app/core/services/`)

| Service | File | Purpose | State | Injection Pattern |
|---------|------|---------|-------|-------------------|
| AuthService | `auth.service.ts` | JWT auth, login, register, logout, token refresh | `isAuthenticated` signal | Constructor |
| ProductService | `product.service.ts` | Fetch products, search, get by ID | Stateless | Constructor |
| CartService | `cart.service.ts` | CRUD cart items, promo codes | `cart` signal | Constructor |
| OrderService | `order.service.ts` | Checkout, order history, payment methods | `orders`, `currentOrder` signals | Constructor |
| ToastService | `toast.service.ts` | User notifications | `toasts` signal | Constructor |

**Note on Injection**: Current services use constructor injection (`constructor(private http: HttpClient)`). This is fine - both patterns work. New code can use either pattern consistently.

### HTTP Interceptors (`src/app/core/interceptors/`)

Configured in `app.config.ts` in this order:
1. **guestSessionInterceptor** - Adds X-Session-Id/X-Session-Secret headers (always, if present)
2. **authInterceptor** - Adds Authorization Bearer token
3. **tokenRefreshInterceptor** - Handles 401s with automatic token refresh
4. **errorInterceptor** - Retry logic, error messages, toast notifications

### Route Guards (`src/app/core/guards/`)

| Guard | Purpose | Current Redirect | After Phase 3 |
|-------|---------|------------------|---------------|
| `authGuard` | Protects routes requiring authentication | `/test` | `/login` |
| `guestGuard` | Prevents authenticated users from guest-only routes | `/test` | `/` |
| `adminGuard` | (Future) Protects admin routes | N/A | `/` with error toast |

### Data Models (`src/app/core/models/`)

```typescript
// Key models - see actual files for full definitions
Product { id: number, name: string|null, description: string|null, price: number,
          stockAmount?: number|null, inStock: boolean, primaryImageUrl: string|null,
          imageUrl?: string|null, category?: string|null, rating?: number|null }

Cart { cartId: number, items: CartItem[], promo: CartPromo|null,
       subtotal: number, discount: number, total: number }

Order { orderId: number, orderDate: string, totalAmount: number, orderStatus: string,
        items: OrderItem[], subtotalAmount: number, discountAmount: number }

ApiErrorResponse { error: string, code?: string, details?: { errors?: ValidationError[], [key: string]: any } }
```

### Current Routes (`src/app/app.routes.ts`)

| Path | Component | Guard | Status |
|------|-----------|-------|--------|
| `/` | Home | - | Done |
| `/shop` | Shop | - | Done |
| `/about` | About | - | Done |
| `/contact` | Contact | - | Done (no API) |
| `/faq` | Faq | - | Done |
| `/checkout` | Checkout | - | Done |
| `/order-confirmation/:orderId` | OrderConfirmation | - | Done |
| `/orders` | Orders | authGuard | Done |
| `/test` | TestPage | - | Dev only |
| `**` | NotFound | - | MISSING |

---

## State & Data Ownership

### Store Architecture

| Store | Location | Signal(s) | Cache Strategy | Refresh Trigger |
|-------|----------|-----------|----------------|-----------------|
| **AuthStore** | `auth.service.ts` | `isAuthenticated` | localStorage tokens | App init, login, logout |
| **GuestSessionStore** | localStorage only | N/A | Persistent until cleared | Cart operations |
| **CartStore** | `cart.service.ts` | `cart` | In-memory signal | Login, add/update/remove item |
| **ProductsStore** | `product.service.ts` | None (stateless) | No cache | Every request |
| **OrdersStore** | `order.service.ts` | `orders`, `currentOrder` | In-memory signal | Checkout, view history |
| **ToastStore** | `toast.service.ts` | `toasts` | In-memory signal | Show/dismiss |

### Data Flow

```
Component --> Service.method() --> HTTP Request
    |                                   |
    |                                   v
    |                            Interceptors (add headers)
    |                                   |
    |                                   v
    |                                 API
    |                                   |
    |                                   v
    |                            Interceptors (handle errors)
    |                                   |
    |              <--------------------+
    |              v
    |         Service updates signal
    |              |
    |              v
    +-------- Component reads signal()
```

### Mapping Layer

API responses are mapped to frontend models in services:
- `ProductService.getProducts()` extracts `items` from paginated response
- `CartService.getCart()` stores full Cart object in signal
- No separate DTO-to-Model transformation layer (direct mapping)

---

## App Initialization & Session Management

### Current Bootstrap (in `auth.service.ts` constructor)

```typescript
constructor(private http: HttpClient) {
  // Check if user is already authenticated on service initialization
  const token = localStorage.getItem('accessToken');
  this.isAuthenticated.set(!!token);
}
```

**Limitation**: No token expiry check, no cart hydration on app start.

### Recommended Bootstrap Sequence (Future Enhancement)

Create `src/app/core/services/app-init.service.ts`:

```typescript
@Injectable({ providedIn: 'root' })
export class AppInitService {
  private authService = inject(AuthService);
  private cartService = inject(CartService);

  initialize(): Observable<void> {
    return new Observable(observer => {
      // 1. Restore auth state from localStorage
      const token = this.authService.getToken();
      if (token) {
        // Note: isTokenExpired() needs to be implemented
        // For now, just set authenticated if token exists
        this.authService.isAuthenticated.set(true);
      }

      // 2. Hydrate cart (will use auth or guest session automatically)
      this.cartService.getCart().subscribe({
        next: () => observer.complete(),
        error: () => observer.complete() // Don't block app on cart error
      });
    });
  }
}
```

**Note**: `isTokenExpired()` method does not exist yet. Either implement it (decode JWT, check exp claim) or skip expiry check and let 401 handling deal with it.

### Token Lifecycle

```
LOGIN SUCCESS
  1. Store tokens in localStorage (accessToken, refreshToken)
  2. isAuthenticated.set(true)
  3. [Optional] clearGuestSession() - currently NOT called
  4. API merges guest cart on next cart request

API REQUEST RETURNS 401
  1. Is this a refresh/login/register request? --> YES --> Throw error
  2. NO --> Try refreshToken()
  3. SUCCESS --> Retry original request with new token
  4. FAILURE --> logout(), redirect to /login (after Phase 3)


LOGOUT
  1. Remove accessToken from localStorage
  2. Remove refreshToken from localStorage
  3. isAuthenticated.set(false)
  4. [TODO] clearGuestSession() - should be added
```

### Multi-Tab Synchronization (Recommended Enhancement)

Add to `AuthService` constructor:

```typescript
// Listen for storage changes from other tabs
window.addEventListener('storage', (event) => {
  if (event.key === 'accessToken') {  // Note: matches actual storage key
    if (event.newValue === null) {
      // Logged out in another tab
      this.isAuthenticated.set(false);
      // Optionally navigate to home
    } else if (event.oldValue === null) {
      // Logged in from another tab
      this.isAuthenticated.set(true);
    }
  }
});
```

---

## Error Handling Specification

### ApiErrorResponse Shape

```typescript
interface ApiErrorResponse {
  error: string;                    // Human-readable message
  code?: string;                    // Machine-readable code (e.g., 'INVALID_CREDENTIALS')
  details?: {
    errors?: ValidationError[];     // Field-level validation errors
    [key: string]: any;
  };
}

interface ValidationError {
  field: string;                    // Form control name (e.g., 'email', 'password')
  message: string;                  // Error message for this field
  code?: string;                    // Optional error code (e.g., 'REQUIRED', 'INVALID_FORMAT')
}
```

### HTTP Status Code Handling

Implemented in `error.interceptor.ts`:

| Status | Behavior | Toast? | Retry? |
|--------|----------|--------|--------|
| 0 | "Unable to connect to server" | Yes | No |
| 401 | Handled by token-refresh interceptor | No | Via refresh |
| 403 | "You do not have permission" | Yes | No |
| 404 | Use `error.message` or default | Yes | No |
| 408 | Retry up to 2x with backoff | Yes (after retries) | Yes |
| 409 | "A conflict occurred" | Yes | No |
| 422 | "Validation failed" - see field errors | See below | No |
| 429 | "Too many requests" | Yes | No |
| 500-504 | Retry up to 2x with backoff | Yes (after retries) | Yes |

### Validation Error Handling (422)

**Priority**: Field errors take precedence over toast. If `details.errors` exists, suppress toast and let component handle inline errors.

**Component handling pattern**:
```typescript
onSubmit() {
  this.apiErrors.set({});  // Clear previous errors

  this.authService.register(this.form.value).subscribe({
    error: (err) => {
      const fieldErrors = err.originalError?.error?.details?.errors;
      if (fieldErrors?.length) {
        // Map field errors to form controls
        const errorMap: Record<string, string> = {};
        for (const e of fieldErrors) {
          errorMap[e.field] = e.message;
          // Optionally set control error: this.form.get(e.field)?.setErrors({ server: e.message });
        }
        this.apiErrors.set(errorMap);
      }
      // Toast already shown by interceptor for non-field errors
    }
  });
}
```

**Template pattern**:
```html
<input formControlName="email" />
@if (apiErrors()['email']) {
  <p class="text-red-600 text-sm">{{ apiErrors()['email'] }}</p>
}
```

### Silent Endpoints

These endpoints don't trigger error toasts (handled by components):
- `/login`
- `/register`
- `/refresh`

For 422 errors with `details.errors`, the toast is also suppressed regardless of endpoint.

### Error Response Transformation

Errors are transformed before being thrown to subscribers:

```typescript
return throwError(() => ({
  message: errorMessage,      // User-friendly message
  originalError: error        // Full HttpErrorResponse for component access
}));
```

### Auth Error Codes (for components to handle)

| Code | Meaning | UI Action |
|------|---------|-----------|
| `INVALID_CREDENTIALS` | Wrong email/password | Show inline error below form |
| `EMAIL_EXISTS` | Registration email taken | Show error on email field |
| `WEAK_PASSWORD` | Password doesn't meet requirements | Show requirements list |
| `INVALID_TOKEN` | Reset token invalid | Show error + link to forgot-password |
| `TOKEN_EXPIRED` | Reset token expired | Show error + link to forgot-password |
| `ACCOUNT_LOCKED` | Too many failed attempts | Show contact support message |

### Correlation ID Handling

**Requirement**: Read `X-Correlation-Id` from error responses and include in:
1. Console error logs (for debugging)
2. Error toasts (append as small text for support reference)

**Implementation** (in `error.interceptor.ts`):
```typescript
const correlationId = error.headers?.get('X-Correlation-Id');
if (correlationId) {
  console.error(`[${correlationId}]`, errorMessage, error);
  if (showToast) {
    toastService.error(`${errorMessage} (Ref: ${correlationId.slice(0, 8)})`);
  }
}
```

### Security: Token and Secret Logging

**NEVER log the following anywhere (console, error tracking, analytics)**:
- Access tokens
- Refresh tokens
- Passwords (plain or hashed)
- Session secrets
- API keys

**Enforcement**:
- Interceptors must not log request/response bodies for auth endpoints
- Error interceptor redacts Authorization header before logging
- No `console.log(token)` or `console.log(password)` in any environment

---

## API Contracts

### Response Types

```typescript
// AuthResponse - returned by /login, /refresh, /api/auth/register
interface AuthResponse {
  tokenType: string;      // "Bearer"
  accessToken: string;
  expiresIn: number;      // seconds until expiry
  refreshToken: string;
}

// RegisterRequest
interface RegisterRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  billingAddress?: string;
  shippingAddress?: string;
}

// PaginatedResponse<T>
interface PaginatedResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}
```

### Authentication Endpoints

#### POST /login
| Aspect | Value |
|--------|-------|
| **Request** | `{ email: string, password: string }` |
| **Response** | `AuthResponse` |
| **Headers** | None required |
| **Success** | 200 - Store tokens, set isAuthenticated |
| **401** | Invalid credentials - Show inline error "Invalid email or password" |
| **422** | Validation errors - Show field errors inline |
| **429** | Rate limited - Show "Too many attempts, try again later" |

#### POST /refresh
| Aspect | Value |
|--------|-------|
| **Request** | `{ refreshToken: string }` |
| **Response** | `AuthResponse` |
| **Headers** | None required |
| **Success** | 200 - Update stored tokens |
| **401** | Invalid/expired refresh token - Call logout(), redirect to /login |

#### POST /api/auth/register
| Aspect | Value |
|--------|-------|
| **Request** | `RegisterRequest` |
| **Response** | `AuthResponse` (auto-login after registration) |
| **Headers** | None required |
| **Success** | 201 - Store tokens, redirect to / |
| **409** | Email exists - Show error on email field |
| **422** | Validation errors - Show field errors inline |

### Product Endpoints

#### GET /api/products
| Aspect | Value |
|--------|-------|
| **Query params** | `?search=string&inStockOnly=boolean&category=string&page=number&pageSize=number` (all optional) |
| **Response** | `PaginatedResponse<Product>` |
| **Headers** | None required |
| **Success** | 200 - Display product grid |
| **400** | Invalid query params - Show toast, display empty state |
| **429** | Rate limited - Show toast, keep current list |
| **500** | Server error - Show toast, display empty state |

#### GET /api/products/{id}
| Aspect | Value |
|--------|-------|
| **Path param** | `id: number` |
| **Response** | `Product` |
| **Headers** | None required |
| **Success** | 200 - Display product detail |
| **404** | Product not found - Show 404 page |

### Cart Endpoints

**Header Requirements** (all cart endpoints):
- **Authenticated**: `Authorization: Bearer {accessToken}`
- **Guest**: `X-Session-Id: {sessionId}`, `X-Session-Secret: {sessionSecret}`
- **Both can be sent**: API prioritizes auth token, falls back to guest session

#### GET /api/cart
| Aspect | Value |
|--------|-------|
| **Response** | `Cart` |
| **Success** | 200 - Update cart signal |
| **401** | Guest session invalid - Clear guest session, let new one be created |

#### POST /api/cart/items
| Aspect | Value |
|--------|-------|
| **Request** | `{ productId: number, quantity: number }` |
| **Response** | `Cart` |
| **Success** | 200 - Update cart signal, show success toast |
| **404** | Product not found - Show error toast |
| **409** | Insufficient stock - Show "Only X available" toast |
| **422** | Invalid quantity - Show validation error |

#### PUT /api/cart/items/{cartItemId}
| Aspect | Value |
|--------|-------|
| **Request** | `{ quantity: number }` |
| **Response** | `Cart` |
| **Success** | 200 - Update cart signal |
| **404** | Item not in cart - Refresh cart |
| **409** | Insufficient stock - Show toast, cap quantity |
| **422** | Invalid quantity - Show validation error |

#### DELETE /api/cart/items/{cartItemId}
| Aspect | Value |
|--------|-------|
| **Response** | `Cart` |
| **Success** | 200 - Update cart signal, show "Item removed" toast |
| **404** | Item not in cart - Refresh cart (no error) |

#### POST /api/cart/promo
| Aspect | Value |
|--------|-------|
| **Request** | `{ promoCode: string }` |
| **Response** | `Cart` |
| **Success** | 200 - Update cart signal, show success toast |
| **404** | Invalid promo code - Show "Invalid promo code" inline error |
| **409** | Promo not applicable - Show reason in toast |

#### DELETE /api/cart/promo
| Aspect | Value |
|--------|-------|
| **Response** | `Cart` |
| **Success** | 200 - Update cart signal |

### Order Endpoints

#### GET /api/orders/my
| Aspect | Value |
|--------|-------|
| **Response** | `Order[]` |
| **Headers** | `Authorization: Bearer {accessToken}` (required) |
| **Success** | 200 - Display order list |
| **401** | Not authenticated - Redirect to /login |

#### GET /api/orders/{orderId}
| Aspect | Value |
|--------|-------|
| **Response** | `Order` |
| **Headers** | `Authorization: Bearer {accessToken}` (required) |
| **Success** | 200 - Display order detail |
| **401** | Not authenticated - Redirect to /login |
| **403** | Not owner of order - Show error, redirect to /orders |
| **404** | Order not found - Show 404 page |

#### POST /api/orders/checkout
| Aspect | Value |
|--------|-------|
| **Request** | `{ shippingAddress: string, paymentMethodId: number }` |
| **Response** | `Order` |
| **Headers** | Auth or Guest headers |
| **Success** | 201 - Clear cart, redirect to /order-confirmation/{orderId} |
| **400** | Cart empty - Show error, redirect to /cart |
| **409** | Stock changed - Show toast, refresh cart |
| **422** | Validation errors - Show field errors inline |

#### POST /api/orders/{orderId}/pay
| Aspect | Value |
|--------|-------|
| **Request** | `{ paymentMethodId: number }` |
| **Response** | `Order` (with updated status) |
| **Headers** | `Authorization: Bearer {accessToken}` (required) |
| **Success** | 200 - Update order status display |
| **402** | Payment failed - Show payment error, allow retry |

#### GET /api/PaymentMethods
| Aspect | Value |
|--------|-------|
| **Response** | `PaymentMethod[]` |
| **Headers** | None required |
| **Success** | 200 - Populate payment method dropdown |

### Endpoints Needed (Backend Work)

| Endpoint | Priority | Request | Response | Purpose |
|----------|----------|---------|----------|---------|
| `GET /api/categories` | High | - | `Category[]` | Product category list |
| `GET /api/users/profile` | High | - | `UserProfile` | Get user profile |
| `PUT /api/users/profile` | High | `UserProfile` | `UserProfile` | Update profile |
| `PUT /api/users/password` | High | `{ current, new }` | `{ success }` | Change password |
| `POST /api/auth/forgot-password` | Medium | `{ email }` | `{ success }` | Initiate reset |
| `POST /api/auth/reset-password` | Medium | `{ token, password }` | `AuthResponse` | Complete reset |
| `POST /api/contact` | Low | `ContactForm` | `{ success }` | Contact form |

---

## Completed Features

### Phase 1: Layout & Navigation - DONE
- [x] Main layout component with navbar and footer
- [x] Responsive navbar with mobile hamburger menu
- [x] Cart badge showing item count
- [x] Authentication state in navbar (login/logout)
- [x] Footer with links and social icons

### Phase 2: Public Pages - DONE
- [x] Home page with hero, features grid, featured products
- [x] Shop page with product grid, pagination, sorting (client-side)
- [x] About page with company story, values, team
- [x] Contact page with form (UI only, no backend)
- [x] FAQ page with accordion categories

### Core Infrastructure - DONE
- [x] JWT authentication with login/register
- [x] Token refresh interceptor for seamless auth
- [x] Guest session management with cart persistence
- [x] Cart merge on login (server-side)
- [x] Toast notification system
- [x] Loading spinner component
- [x] Error handling with retry logic
- [x] Route guards for protected pages

### Shopping Flow - DONE
- [x] Product card component with quantity selector
- [x] Add to cart functionality
- [x] Checkout page with shipping/payment
- [x] Order confirmation page
- [x] Order history page

---

## Remaining Implementation Tasks

### Phase 3: Authentication Pages (HIGH PRIORITY)

| Task | Description | Complexity | Dependencies |
|------|-------------|------------|--------------|
| 3.1 Login Page | Dedicated `/login` route | Medium | None |
| 3.2 Register Page | `/register` route | Medium | None |
| 3.3 Forgot Password | `/forgot-password` | Easy | API endpoint |
| 3.4 Reset Password | `/reset-password?token=xxx` | Medium | API endpoint |
| 3.5 User Profile | `/account` | High | API endpoint |
| 3.6 Update authGuard | Redirect to `/login` | Easy | 3.1 |
| 3.7 Add 404 Page | Catch-all route | Easy | None |
| 3.8 Update logout | Clear guest session | Easy | None |

### Phase 4: Shopping Experience (HIGH PRIORITY)

| Task | Description | Complexity | Dependencies |
|------|-------------|------------|--------------|
| 4.1 Cart Page | Dedicated `/cart` page | High | None |
| 4.2 Cart Drawer | Slide-out cart (optional) | Medium | 4.1 |
| 4.3 Product Detail | `/product/:id` page | High | None |
| 4.4 Search | Navbar search with dropdown | Medium | None |
| 4.5 Category Filter | Shop page filter | Medium | API endpoint |
| 4.6 Price Filter | Shop page price range | Easy | None |
| 4.7 URL State | Persist filters in URL | Medium | 4.4, 4.5 |

### Phase 5: Policy Pages (LOW PRIORITY)

| Task | Description | Complexity |
|------|-------------|------------|
| 5.1 Refund Policy | `/refund-policy` | Easy |
| 5.2 Shipping & Returns | `/shipping` | Easy |
| 5.3 Terms of Service | `/terms` | Easy |
| 5.4 Privacy Policy | `/privacy` | Easy |

### Phase 6: Admin Dashboard (MEDIUM PRIORITY)

| Task | Description | Complexity | Dependencies |
|------|-------------|------------|--------------|
| 6.1 Admin Layout | Sidebar layout | Medium | Admin role |
| 6.2 Admin Guard | Role-based protection | Medium | API role claims |
| 6.3 Dashboard | Stats overview | High | Admin API |
| 6.4 Product CRUD | Manage products | High | Admin API |
| 6.5 Order Management | View/update orders | High | Admin API |
| 6.6 User Management | View users | Medium | Admin API |
| 6.7 Promo Management | Create promos | Medium | Admin API |

### Phase 7: Advanced Features (LOW PRIORITY)

| Task | Description | Complexity | Dependencies |
|------|-------------|------------|--------------|
| 7.1 Product Reviews | Submit/display reviews | High | API endpoints |
| 7.2 Wishlist | Save for later | Medium | API endpoints |
| 7.3 Analytics | Google Analytics | Easy | GA account |
| 7.4 Error Monitoring | Sentry integration | Easy | Sentry account |
| 7.5 PWA Support | Service worker | Medium | None |

---

## Phase Checklists

### Phase 3 Checklist: Authentication Pages

#### 3.1 Login Page
- [ ] Create `src/app/features/auth/login/` (login.ts, login.html, login.css)
- [ ] **Layout**: Standalone (NO MainLayout - no navbar/footer)
- [ ] **Form fields**: email, password, remember me checkbox
- [ ] **Validation**: Required, email format, min password length
- [ ] **Error handling**:
  - [ ] `INVALID_CREDENTIALS` -> inline error
  - [ ] `ACCOUNT_LOCKED` -> specific message
  - [ ] Network error -> toast (handled by interceptor)
- [ ] **Post-login redirect**: Check `localStorage.returnUrl` or default to `/`
- [ ] **Links**: Forgot password, Register
- [ ] Add route: `{ path: 'login', component: Login, canActivate: [guestGuard] }`
- [ ] Add nav link (when not authenticated)
- [ ] Test: valid/invalid credentials
- [ ] Test: redirect after login

#### 3.2 Register Page
- [ ] Create `src/app/features/auth/register/`
- [ ] **Layout**: Standalone (no MainLayout)
- [ ] **Form fields**: firstName, lastName, email, password, confirmPassword
- [ ] **Validation**: Required, email format, password 8+ chars, passwords match
- [ ] **Error handling**: `EMAIL_EXISTS`, `WEAK_PASSWORD` -> inline errors
- [ ] **Post-register**: Auto-login and redirect to `/`
- [ ] **Terms checkbox**: Required
- [ ] Add route with guestGuard
- [ ] Test: register, duplicate email

#### 3.3 Forgot Password Page
- [ ] Create component with email input
- [ ] **Layout**: Standalone
- [ ] Call API: `POST /api/auth/forgot-password { email }`
- [ ] **Success**: "Check your email" (don't reveal if email exists)
- [ ] **Rate limiting**: Disable button for 60s after submit
- [ ] Add route with guestGuard

#### 3.4 Reset Password Page
- [ ] Create component with password + confirm fields
- [ ] **Layout**: Standalone
- [ ] Read token from URL: `/reset-password?token=xxx`
- [ ] Call API: `POST /api/auth/reset-password { token, newPassword }`
- [ ] **Errors**: `INVALID_TOKEN`, `TOKEN_EXPIRED` -> link to forgot-password
- [ ] **Success**: Message + link to login
- [ ] Add route (no guard - token validates)

#### 3.5 User Profile Page
- [ ] Create `src/app/features/account/`
- [ ] **Layout**: Use MainLayout
- [ ] **Sections**: Profile info, addresses, change password, order history link
- [ ] **API calls**: GET/PUT profile, PUT password
- [ ] Add route with authGuard
- [ ] Add nav link (when authenticated)

#### 3.6 Update authGuard
- [ ] Change redirect from `/test` to `/login`
- [ ] Store returnUrl: `localStorage.setItem('returnUrl', state.url)`

#### 3.7 Add 404 Page
- [ ] Create `src/app/features/not-found/`
- [ ] **Layout**: Use MainLayout
- [ ] **Content**: "Page not found", link to home
- [ ] Add catch-all route: `{ path: '**', component: NotFound }`

#### 3.8 Update logout
- [ ] In `auth.service.ts` `logout()`, add `this.clearGuestSession()`

#### Phase 3 Done Criteria
- [ ] All routes in `app.routes.ts`
- [ ] Navbar shows Login/Register or Account based on auth state
- [ ] `/test` page can be hidden/removed in production
- [ ] All forms have loading, error, success states
- [ ] `npm run build` passes

---

### Phase 4 Checklist: Shopping Experience

#### 4.1 Cart Page
- [ ] Create `src/app/features/cart/`
- [ ] **Layout**: Use MainLayout
- [ ] **Empty state**: "Your cart is empty" with Shop link
- [ ] **Cart items**: image, name, price, quantity (stock-aware), remove, line total
- [ ] **Order summary**: subtotal, discount, promo input, total (all from API)
- [ ] **Checkout button**: Link to `/checkout`
- [ ] **Loading state**: Skeleton loader
- [ ] Add route: `{ path: 'cart', component: Cart }`
- [ ] Update navbar cart icon to link to `/cart`

#### 4.2 Cart Drawer (Optional)
- [ ] Create slide-out drawer component
- [ ] **State**: `isCartDrawerOpen` signal in CartService
- [ ] **Body scroll lock**: `document.body.style.overflow`
- [ ] **Close**: X button, click outside, ESC key
- [ ] **Mini cart view**: Items, total, View Cart/Checkout buttons

#### 4.3 Product Detail Page
- [ ] Create `src/app/features/product-detail/`
- [ ] **Layout**: Use MainLayout
- [ ] **404 handling**: Show NotFound if product doesn't exist or ID is NaN
- [ ] **Product info**: Image(s), name, price, rating, stock badge, quantity, add-to-cart
- [ ] **Tabs**: Description, Ingredients (if applicable)
- [ ] **Related products**: 4 from same category
- [ ] Add route: `{ path: 'product/:id', component: ProductDetail }`
- [ ] Make product cards clickable

#### 4.4 Search Functionality
- [ ] Add search input to navbar
- [ ] **Debounce**: 300ms with RxJS `debounceTime`
- [ ] **Dropdown**: Top 5 results
- [ ] **Full results**: `/search?q=term` route
- [ ] **No results state**: "No products found"
- [ ] **URL state**: Query in URL

#### 4.5 Category Filter
- [ ] Add category filter to Shop page
- [ ] **API**: `GET /api/categories` (needs backend)
- [ ] **Filter**: `GET /api/products?category=X`
- [ ] **URL state**: `/shop?category=xxx`
- [ ] **Clear filter**: "All Categories" option

#### 4.6 Price Filter
- [ ] Add price range inputs
- [ ] **Client-side filtering**: Filter loaded products
- [ ] **URL state**: `/shop?minPrice=10&maxPrice=50`

#### 4.7 URL State for Filters
- [ ] Sync all filters with URL query params
- [ ] **On load**: Read params, apply filters
- [ ] **On change**: Update URL without reload (`router.navigate`)
- [ ] **Back/forward**: Restore filter state

#### Phase 4 Done Criteria
- [ ] Cart page fully functional
- [ ] Product detail shows all info
- [ ] Search returns relevant results
- [ ] Filters persist in URL
- [ ] All empty/error/loading states
- [ ] `npm run build` passes

---

## Angular Code Constraints

### Template Rules

1. **No inline data-URI SVGs in style attributes** - Angular template parser chokes on complex URLs with quotes
   ```html
   <!-- BAD -->
   <div style="background-image: url('data:image/svg+xml,...')"></div>

   <!-- GOOD - use CSS class -->
   <div class="hero-pattern"></div>
   ```

2. **Normalize nullable types before binding** - Use nullish coalescing or safe navigation
   ```html
   <!-- Handle undefined/null -->
   {{ product.name ?? 'Unknown' }}
   {{ product.rating ?? 0 | number:'1.1-1' }}

   <!-- For method calls with nullable params -->
   getStars(product.rating)  // Method must accept number | null | undefined
   ```

3. **Track expressions in @for loops**
   ```html
   @for (item of items; track item.id) { ... }
   @for (star of stars; track $index) { ... }
   ```

### TypeScript Rules

1. **Strict null checks** - All models have nullable fields; handle accordingly
2. **Signal patterns** - Use `signal()`, `.set()`, `.update()`, `computed()`
3. **Injection** - Either constructor or `inject()` is fine, be consistent per file
4. **Async handling** - Use RxJS operators, avoid nested subscribes

### File Naming

- Components: `feature-name.ts`, `feature-name.html`, `feature-name.css`
- Services: `feature.service.ts`
- Guards: `feature.guard.ts`
- Interceptors: `feature.interceptor.ts`
- Models: `feature.models.ts`

---

## Styling & Design System

### Tailwind Configuration (`tailwind.config.js`)

```javascript
module.exports = {
  content: ["./src/**/*.{html,ts}"],
  theme: {
    extend: {
      colors: {
        primary: { /* amber shades 50-950 */ },
        accent: { /* rose shades 50-950 */ },
        brand: { /* violet shades 50-950 */ }
      },
      fontFamily: {
        sans: ['Inter', 'system-ui', 'sans-serif'],
        display: ['Poppins', 'Inter', 'system-ui', 'sans-serif']
      }
    }
  }
}
```

### Global CSS Classes (`src/styles.css`)

| Class | Purpose |
|-------|---------|
| `.btn-primary` | Primary action buttons (amber) |
| `.btn-secondary` | Secondary actions (slate) |
| `.btn-outline` | Outlined buttons |
| `.btn-ghost` | Minimal buttons |
| `.btn-danger` | Destructive actions (red) |
| `.input` | Form inputs |
| `.input-error` | Input with error state |
| `.card` | Card container |
| `.card-hover` | Card with hover effects |
| `.container-wide` | Max-width container |
| `.empty-state` | Empty state container |
| `.badge`, `.badge-success`, `.badge-warning`, `.badge-error` | Status badges |

---

## Testing Strategy

### Unit Tests Required

| Area | File | Tests |
|------|------|-------|
| Auth | `auth.service.spec.ts` | login, logout, refresh, token storage |
| Auth | `auth.guard.spec.ts` | redirect when unauthed, pass when authed, returnUrl storage |
| Cart | `cart.service.spec.ts` | add, update, remove, promo codes, signal updates |
| Guest | `guest-session.interceptor.spec.ts` | header injection, header capture from response |
| Refresh | `token-refresh.interceptor.spec.ts` | refresh on 401, retry, logout on failure |
| Cart Merge | Integration test | Guest cart + login = merged cart |

### E2E Tests (Critical Paths)

1. **Auth Flow**: Register -> Login -> Logout
2. **Guest Checkout**: Add to cart -> Checkout -> Confirmation
3. **Authenticated Checkout**: Login -> Add to cart -> Checkout -> View history
4. **Cart Merge**: Add as guest -> Login -> Verify merged cart

### Manual Testing Checklist

Before each phase completion:
- [ ] All routes load without errors
- [ ] Mobile responsive (375px)
- [ ] Loading states during API calls
- [ ] Error states show user-friendly messages
- [ ] Empty states have clear CTAs
- [ ] Forms validate on blur and submit
- [ ] `npm run build` passes
- [ ] No console errors in browser

---

## Deployment Considerations

### Environment Configuration

**Development** (`environment.ts`):
```typescript
export const environment = {
  production: false,
  apiUrl: 'https://localhost:7058/api',
  apiBaseUrl: 'https://localhost:7058'
};
```

**Production** (`environment.prod.ts`):
```typescript
export const environment = {
  production: true,
  apiUrl: 'https://api.sobee.com/api',
  apiBaseUrl: 'https://api.sobee.com'
};
```

### SPA Routing (nginx)

```nginx
server {
  listen 80;
  root /usr/share/nginx/html;
  index index.html;

  location / {
    try_files $uri $uri/ /index.html;
  }

  location ~* \.(js|css|png|jpg|jpeg|gif|ico|svg|woff|woff2)$ {
    expires 1y;
    add_header Cache-Control "public, immutable";
  }

  location = /index.html {
    expires -1;
    add_header Cache-Control "no-store, no-cache, must-revalidate";
  }
}
```

### CORS (Backend)

```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("https://sobee.com", "http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod();
              // Note: AllowCredentials() not needed since we use Bearer tokens
    });
});
```

---

## Open Questions & Decisions Log

### Resolved Decisions

| Question | Decision | Date |
|----------|----------|------|
| Token storage | localStorage (`accessToken`, `refreshToken`) | Jan 2026 |
| Product IDs | Numeric integers | Jan 2026 |
| Cart totals | Server-authoritative | Jan 2026 |
| Auth page layout | Standalone (no MainLayout) | Jan 2026 |
| Injection pattern | Constructor OK, inject() OK - be consistent per file | Jan 2026 |
| SSR | No - client-side only | Jan 2026 |
| i18n | English only, no translation infrastructure | Jan 2026 |
| Currency | USD only; format using CurrencyPipe or Intl.NumberFormat | Jan 2026 |
| withCredentials | false - using Bearer tokens | Jan 2026 |
| Cart UI updates | Server-confirmed (not optimistic) | Jan 2026 |

### Open Questions (Need User Input)

| # | Question | Options | Default |
|---|----------|---------|---------|
| 1 | Admin: same app or separate? | Same (lazy loaded) / Separate | Same app |
| 2 | Guest checkout email required? | Yes / No | Yes |
| 3 | Wishlist for guests? | Auth only / Guest with merge | Auth only |
| 4 | Payment provider? | Stripe / PayPal / Both / Mock | TBD |
| 5 | Review moderation? | Auto-publish / Admin approval | Auto-publish |

### Backend Work Tracker

| Endpoint | Priority | Status |
|----------|----------|--------|
| `GET /api/categories` | High | Not started |
| `GET /api/users/profile` | High | Not started |
| `PUT /api/users/profile` | High | Not started |
| `PUT /api/users/password` | High | Not started |
| `POST /api/auth/forgot-password` | Medium | Not started |
| `POST /api/auth/reset-password` | Medium | Not started |
| `POST /api/contact` | Low | Not started |
| Admin endpoints | Low | Not started |

### Code Debt / TODOs

| Item | Location | Priority |
|------|----------|----------|
| Add `clearGuestSession()` to `logout()` | `auth.service.ts` | High |
| Implement `isTokenExpired()` or rely on 401 handling | `auth.service.ts` | Medium |
| Add multi-tab sync for auth state | `auth.service.ts` | Medium |
| Suppress guest headers when authenticated | `guest-session.interceptor.ts` | Low |
| Add APP_INITIALIZER for cart hydration | `app.config.ts` | Low |

---

## Quick Reference: File Locations

### Core Files
| Purpose | Path |
|---------|------|
| App entry | `src/main.ts` |
| App config | `src/app/app.config.ts` |
| Routes | `src/app/app.routes.ts` |
| Global styles | `src/styles.css` |
| Tailwind config | `tailwind.config.js` |
| Environment | `src/environments/environment.ts` |

### Services
| Service | Path |
|---------|------|
| Auth | `src/app/core/services/auth.service.ts` |
| Products | `src/app/core/services/product.service.ts` |
| Cart | `src/app/core/services/cart.service.ts` |
| Orders | `src/app/core/services/order.service.ts` |
| Toast | `src/app/core/services/toast.service.ts` |

### Shared Components
| Component | Path |
|-----------|------|
| Navbar | `src/app/shared/components/navbar/` |
| Footer | `src/app/shared/components/footer/` |
| Product Card | `src/app/shared/components/product-card/` |
| Toast | `src/app/shared/components/toast/` |
| Loading Spinner | `src/app/shared/components/loading-spinner/` |
| Main Layout | `src/app/shared/layout/main-layout.ts` |

---

## Notes for LLM Context

When continuing this project:

1. **Read files before editing** - Codebase uses standalone components with specific patterns
2. **Use Angular Signals** - `signal()`, `.set()`, `.update()`, `computed()`
3. **Follow existing patterns** - Check `home.ts` for page component structure
4. **Tailwind classes** - Use existing utilities from `styles.css`
5. **Injection** - Constructor injection is used currently; either pattern is OK
6. **API integration** - All HTTP through services; interceptors handle auth/errors
7. **Toast notifications** - Use `ToastService` for user feedback
8. **MainLayout** - Public pages use it; auth pages do NOT
9. **Build verification** - Run `npm run build` after changes
10. **Phase checklists** - Each phase has detailed done criteria
11. **URL state** - Search/filters should persist in query params
12. **Empty/error/loading states** - Every data view needs all three
13. **No inline SVG data URIs** - Use CSS classes instead
14. **Handle nullable types** - Use `??` and type guards
15. **Storage keys** - `accessToken`, `refreshToken`, `guestSessionId`, `guestSessionSecret`

---

*Last Updated: January 2026*
*Document Version: 3.1*
