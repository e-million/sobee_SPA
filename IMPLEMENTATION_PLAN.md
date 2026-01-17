# Sobee Angular SPA - Complete Implementation Plan

> **Purpose**: This document serves as a comprehensive guide for continuing development of the Sobee e-commerce Angular application. It is designed to be used as context for an LLM assistant or developer to understand the current state, remaining work, and implementation details.

---

## Table of Contents
1. [Project Overview](#project-overview)
2. [Current Architecture](#current-architecture)
3. [Completed Features](#completed-features)
4. [Remaining Implementation Tasks](#remaining-implementation-tasks)
5. [Detailed Implementation Specifications](#detailed-implementation-specifications)
6. [API Integration Notes](#api-integration-notes)
7. [Styling & Design System](#styling--design-system)
8. [Testing Strategy](#testing-strategy)
9. [Deployment Considerations](#deployment-considerations)

---

## Project Overview

### What is Sobee?
Sobee is an e-commerce platform for a non-alcoholic aperitif/energy drink company. The project is a modernization effort, migrating from an ASP.NET MVC monolith to a decoupled architecture:
- **Backend**: ASP.NET Core Web API (located in `sobee_Core/`)
- **Frontend**: Angular 20 SPA with Tailwind CSS (located in `sobee_Client/`)

### Repository Structure
```
sobee_SPA/
├── sobee_Client/          # Angular frontend application
│   ├── src/
│   │   ├── app/
│   │   │   ├── core/      # Services, models, guards, interceptors
│   │   │   ├── features/  # Page components (home, shop, checkout, etc.)
│   │   │   └── shared/    # Reusable components (navbar, footer, product-card)
│   │   ├── environments/  # Environment configuration
│   │   └── styles.css     # Global Tailwind styles
│   ├── tailwind.config.js
│   └── package.json
├── sobee_Core/            # ASP.NET Core API
├── FEATURE_ROADMAP.md     # Original feature analysis
└── IMPLEMENTATION_PLAN.md # This document
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

## Current Architecture

### Core Services (`src/app/core/services/`)

| Service | File | Purpose | State |
|---------|------|---------|-------|
| AuthService | `auth.service.ts` | JWT auth, login, register, logout, token refresh | `isAuthenticated` signal |
| ProductService | `product.service.ts` | Fetch products, search, get by ID | Stateless |
| CartService | `cart.service.ts` | CRUD cart items, promo codes | `cart` signal |
| OrderService | `order.service.ts` | Checkout, order history, payment methods | `orders`, `currentOrder` signals |
| ToastService | `toast.service.ts` | User notifications | `toasts` signal |

### HTTP Interceptors (`src/app/core/interceptors/`)

Configured in `app.config.ts` in this order:
1. **guestSessionInterceptor** - Adds X-Session-Id/X-Session-Secret headers for guests
2. **authInterceptor** - Adds Authorization Bearer token
3. **tokenRefreshInterceptor** - Handles 401s with automatic token refresh
4. **errorInterceptor** - Retry logic, error messages, toast notifications

### Route Guards (`src/app/core/guards/`)

| Guard | Purpose |
|-------|---------|
| `authGuard` | Protects routes requiring authentication, redirects to /test |
| `guestGuard` | Prevents authenticated users from accessing guest-only routes |

### Data Models (`src/app/core/models/`)

```typescript
// Key models - see actual files for full definitions
Product { id, name, description, price, stockAmount, inStock, primaryImageUrl, imageUrl, category, rating }
Cart { cartId, items: CartItem[], promo, subtotal, discount, total }
Order { orderId, orderDate, totalAmount, orderStatus, items: OrderItem[] }
UserProfile { email, firstName, lastName, billingAddress, shippingAddress }
```

### Current Routes (`src/app/app.routes.ts`)

| Path | Component | Guard | Status |
|------|-----------|-------|--------|
| `/` | Home | - | ✅ Implemented |
| `/shop` | Shop | - | ✅ Implemented |
| `/about` | About | - | ✅ Implemented |
| `/contact` | Contact | - | ✅ Implemented (no API) |
| `/faq` | Faq | - | ✅ Implemented |
| `/checkout` | Checkout | - | ✅ Implemented |
| `/order-confirmation/:orderId` | OrderConfirmation | - | ✅ Implemented |
| `/orders` | Orders | authGuard | ✅ Implemented |
| `/test` | TestPage | - | ✅ Dev testing page |

---

## Completed Features

### Phase 1: Layout & Navigation ✅
- [x] Main layout component with navbar and footer
- [x] Responsive navbar with mobile hamburger menu
- [x] Cart badge showing item count
- [x] Authentication state in navbar (login/logout)
- [x] Footer with links and social icons

### Phase 2: Public Pages ✅
- [x] Home page with hero, features grid, featured products
- [x] Shop page with product grid, pagination, sorting (client-side)
- [x] About page with company story, values, team
- [x] Contact page with form (UI only, no backend)
- [x] FAQ page with accordion categories

### Core Infrastructure ✅
- [x] JWT authentication with login/register
- [x] Token refresh interceptor for seamless auth
- [x] Guest session management with cart persistence
- [x] Cart merge on login
- [x] Toast notification system
- [x] Loading spinner component
- [x] Error handling with retry logic
- [x] Route guards for protected pages

### Shopping Flow ✅
- [x] Product card component with quantity selector
- [x] Add to cart functionality
- [x] Checkout page with shipping/payment
- [x] Order confirmation page
- [x] Order history page

---

## Remaining Implementation Tasks

### Phase 3: Authentication Pages (HIGH PRIORITY)
> Currently, auth is only available via the `/test` page. Need dedicated auth pages.

| Task | Description | Complexity | Dependencies |
|------|-------------|------------|--------------|
| 3.1 Login Page | Dedicated `/login` route with styled form, remember me, forgot password link | Medium | None |
| 3.2 Register Page | `/register` route with validation, terms acceptance | Medium | None |
| 3.3 Forgot Password | `/forgot-password` - email input to trigger reset | Easy | API endpoint needed |
| 3.4 Reset Password | `/reset-password?token=xxx` - new password form | Medium | API endpoint needed |
| 3.5 User Profile | `/account` or `/profile` - view/edit profile, change password | High | API endpoint needed |

**Implementation Notes:**
- Update `authGuard` to redirect to `/login` instead of `/test`
- Add `guestGuard` to login/register to redirect authenticated users to home
- Store intended URL for post-login redirect
- Consider social login (Google, Apple) in future

### Phase 4: Shopping Experience Enhancements (HIGH PRIORITY)

| Task | Description | Complexity | Dependencies |
|------|-------------|------------|--------------|
| 4.1 Cart Page | Dedicated `/cart` page with full cart management | High | None |
| 4.2 Cart Sidebar | Optional slide-out cart drawer | Medium | Cart Page |
| 4.3 Product Detail | `/product/:id` page with full details, images, reviews | High | API: GET /api/products/{id} |
| 4.4 Search | Search bar in navbar with results dropdown | Medium | Uses existing ProductService |
| 4.5 Category Filter | Filter products by category on shop page | Medium | API: categories endpoint |
| 4.6 Price Filter | Price range slider on shop page | Easy | Client-side filtering |

**Implementation Notes for Cart Page:**
```
Layout:
┌─────────────────────────┬────────────┐
│ CART ITEMS              │ ORDER      │
│ ┌─────────────────────┐ │ SUMMARY    │
│ │ [IMG] Name    $XX   │ │            │
│ │       Qty: [- 2 +]  │ │ Subtotal   │
│ │       [Remove]      │ │ Shipping   │
│ └─────────────────────┘ │ Tax        │
│                         │ ──────────  │
│                         │ Total      │
│                         │            │
│                         │ [PROMO]    │
│                         │ [CHECKOUT] │
└─────────────────────────┴────────────┘
```

**Implementation Notes for Product Detail Page:**
- Route: `/product/:id` or `/shop/:id`
- Fetch product via `ProductService.getProduct(id)`
- Image gallery with thumbnails
- Quantity selector
- Add to cart button
- Product description tabs (Description, Ingredients, Reviews)
- Related products section

### Phase 5: Policy & Static Pages (LOW PRIORITY)

| Task | Description | Complexity |
|------|-------------|------------|
| 5.1 Refund Policy | `/refund-policy` static content | Easy |
| 5.2 Shipping & Returns | `/shipping` static content | Easy |
| 5.3 Terms of Service | `/terms` static content | Easy |
| 5.4 Privacy Policy | `/privacy` static content | Easy |

**Implementation Notes:**
- Create a reusable `StaticPage` component
- Store content as markdown or HTML in component
- Use MainLayout wrapper
- Add links from footer

### Phase 6: Admin Dashboard (MEDIUM PRIORITY)

| Task | Description | Complexity | Dependencies |
|------|-------------|------------|--------------|
| 6.1 Admin Layout | Separate layout with sidebar | Medium | Admin role in API |
| 6.2 Dashboard | Stats overview (orders, revenue, users) | High | Admin API endpoints |
| 6.3 Product Management | CRUD products, upload images | High | Admin API endpoints |
| 6.4 Order Management | View orders, update status | High | Admin API endpoints |
| 6.5 User Management | View users, roles | Medium | Admin API endpoints |
| 6.6 Promo Management | Create/manage promo codes | Medium | Admin API endpoints |

**Implementation Notes:**
- Create `/admin` route prefix with admin guard
- Separate lazy-loaded module for admin
- Use different layout (sidebar navigation)
- Requires admin role check from API
- Consider using a table component library (Angular Material or PrimeNG)

### Phase 7: Advanced Features (LOW PRIORITY)

| Task | Description | Complexity | Dependencies |
|------|-------------|------------|--------------|
| 7.1 Product Reviews | Submit/display reviews with ratings | High | API: reviews endpoints |
| 7.2 Wishlist | Save products for later | Medium | API: wishlist endpoints |
| 7.3 Email Notifications | Order confirmations (backend) | Medium | Backend implementation |
| 7.4 Social Integration | Share buttons, Instagram feed | Medium | Social APIs |
| 7.5 Analytics | Google Analytics integration | Easy | GA account |
| 7.6 PWA Support | Offline support, installable | Medium | Service worker |

---

## Detailed Implementation Specifications

### 3.1 Login Page Implementation

**File Structure:**
```
src/app/features/auth/
├── login/
│   ├── login.ts
│   ├── login.html
│   └── login.css
├── register/
│   ├── register.ts
│   ├── register.html
│   └── register.css
└── (other auth pages)
```

**Login Component Template:**
```html
<div class="min-h-screen flex items-center justify-center bg-slate-50">
  <div class="max-w-md w-full bg-white rounded-xl shadow-lg p-8">
    <div class="text-center mb-8">
      <h1 class="text-2xl font-bold text-slate-800">Welcome Back</h1>
      <p class="text-slate-500">Sign in to your SoBee account</p>
    </div>

    <form [formGroup]="loginForm" (ngSubmit)="onSubmit()">
      <div class="space-y-4">
        <div>
          <label class="block text-sm font-medium text-slate-700 mb-1">Email</label>
          <input type="email" formControlName="email" class="input w-full" />
          <!-- Validation errors -->
        </div>

        <div>
          <label class="block text-sm font-medium text-slate-700 mb-1">Password</label>
          <input type="password" formControlName="password" class="input w-full" />
        </div>

        <div class="flex items-center justify-between">
          <label class="flex items-center">
            <input type="checkbox" formControlName="rememberMe" class="rounded" />
            <span class="ml-2 text-sm text-slate-600">Remember me</span>
          </label>
          <a routerLink="/forgot-password" class="text-sm text-primary-600 hover:underline">
            Forgot password?
          </a>
        </div>

        <button type="submit" [disabled]="loading()" class="btn-primary w-full">
          {{ loading() ? 'Signing in...' : 'Sign In' }}
        </button>
      </div>
    </form>

    <p class="text-center mt-6 text-slate-600">
      Don't have an account?
      <a routerLink="/register" class="text-primary-600 hover:underline">Sign up</a>
    </p>
  </div>
</div>
```

**Login Component Logic:**
```typescript
@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './login.html'
})
export class Login {
  private authService = inject(AuthService);
  private router = inject(Router);
  private toastService = inject(ToastService);

  loading = signal(false);

  loginForm = new FormGroup({
    email: new FormControl('', [Validators.required, Validators.email]),
    password: new FormControl('', [Validators.required, Validators.minLength(6)]),
    rememberMe: new FormControl(false)
  });

  onSubmit() {
    if (this.loginForm.invalid) return;

    this.loading.set(true);
    const { email, password } = this.loginForm.value;

    this.authService.login({ email: email!, password: password! }).subscribe({
      next: () => {
        this.toastService.success('Welcome back!');
        const returnUrl = localStorage.getItem('returnUrl') || '/';
        localStorage.removeItem('returnUrl');
        this.router.navigate([returnUrl]);
      },
      error: (err) => {
        this.loading.set(false);
        this.toastService.error(err.message || 'Login failed');
      }
    });
  }
}
```

### 4.1 Cart Page Implementation

**Cart Page Component:**
```typescript
@Component({
  selector: 'app-cart',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, MainLayout],
  templateUrl: './cart.html'
})
export class CartPage implements OnInit {
  private cartService = inject(CartService);
  private toastService = inject(ToastService);

  cart = this.cartService.cart;
  loading = signal(false);
  promoCode = signal('');
  applyingPromo = signal(false);

  ngOnInit() {
    this.loadCart();
  }

  loadCart() {
    this.loading.set(true);
    this.cartService.getCart().subscribe({
      next: () => this.loading.set(false),
      error: () => {
        this.toastService.error('Failed to load cart');
        this.loading.set(false);
      }
    });
  }

  updateQuantity(cartItemId: number, quantity: number) {
    if (quantity < 1 || quantity > 10) return;
    this.cartService.updateItem(cartItemId, { quantity }).subscribe({
      error: () => this.toastService.error('Failed to update quantity')
    });
  }

  removeItem(cartItemId: number) {
    this.cartService.removeItem(cartItemId).subscribe({
      next: () => this.toastService.success('Item removed'),
      error: () => this.toastService.error('Failed to remove item')
    });
  }

  applyPromoCode() {
    const code = this.promoCode().trim();
    if (!code) return;

    this.applyingPromo.set(true);
    this.cartService.applyPromo({ promoCode: code }).subscribe({
      next: () => {
        this.toastService.success('Promo code applied!');
        this.applyingPromo.set(false);
      },
      error: () => {
        this.toastService.error('Invalid promo code');
        this.applyingPromo.set(false);
      }
    });
  }

  removePromoCode() {
    this.cartService.removePromo().subscribe({
      next: () => this.toastService.success('Promo code removed'),
      error: () => this.toastService.error('Failed to remove promo')
    });
  }
}
```

### 4.3 Product Detail Page Implementation

**Route Configuration:**
```typescript
{ path: 'product/:id', component: ProductDetail }
// or
{ path: 'shop/:id', component: ProductDetail }
```

**Product Detail Component:**
```typescript
@Component({
  selector: 'app-product-detail',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, MainLayout, ProductCard],
  templateUrl: './product-detail.html'
})
export class ProductDetail implements OnInit {
  private route = inject(ActivatedRoute);
  private productService = inject(ProductService);
  private cartService = inject(CartService);
  private toastService = inject(ToastService);

  product = signal<Product | null>(null);
  relatedProducts = signal<Product[]>([]);
  loading = signal(true);
  quantity = signal(1);
  selectedTab = signal<'description' | 'ingredients' | 'reviews'>('description');

  ngOnInit() {
    this.route.params.subscribe(params => {
      const id = +params['id'];
      this.loadProduct(id);
    });
  }

  loadProduct(id: number) {
    this.loading.set(true);
    this.productService.getProduct(id).subscribe({
      next: (product) => {
        this.product.set(product);
        this.loading.set(false);
        this.loadRelatedProducts(product.category);
      },
      error: () => {
        this.toastService.error('Product not found');
        this.loading.set(false);
      }
    });
  }

  loadRelatedProducts(category?: string | null) {
    // Load 4 products from same category, excluding current
    this.productService.getProducts().subscribe({
      next: (products) => {
        const related = products
          .filter(p => p.id !== this.product()?.id)
          .filter(p => !category || p.category === category)
          .slice(0, 4);
        this.relatedProducts.set(related);
      }
    });
  }

  addToCart() {
    const product = this.product();
    if (!product) return;

    this.cartService.addItem({
      productId: product.id,
      quantity: this.quantity()
    }).subscribe({
      next: () => {
        this.toastService.success(`Added ${this.quantity()} ${product.name} to cart!`);
        this.quantity.set(1);
      },
      error: () => this.toastService.error('Failed to add to cart')
    });
  }
}
```

---

## API Integration Notes

### Current API Endpoints (Backend: https://localhost:7058)

**Authentication:**
```
POST /login                    # Login, returns JWT tokens
POST /refresh                  # Refresh access token
POST /api/auth/register        # Register new user
```

**Products:**
```
GET /api/products              # List all products (paginated)
    ?search=term               # Optional search filter
    ?inStockOnly=true          # Optional stock filter
GET /api/products/{id}         # Get single product
```

**Cart:**
```
GET    /api/cart               # Get current cart
POST   /api/cart/items         # Add item { productId, quantity }
PUT    /api/cart/items/{id}    # Update item { quantity }
DELETE /api/cart/items/{id}    # Remove item
POST   /api/cart/promo         # Apply promo { promoCode }
DELETE /api/cart/promo         # Remove promo
```

**Orders:**
```
GET  /api/orders/my            # Get user's orders (authenticated)
GET  /api/orders/{id}          # Get order by ID
POST /api/orders/checkout      # Create order { shippingAddress, paymentMethodId }
POST /api/orders/{id}/pay      # Process payment
GET  /api/PaymentMethods       # Get available payment methods
```

### API Endpoints Needed (Backend Work Required)

```
# User Profile
GET  /api/users/profile        # Get current user profile
PUT  /api/users/profile        # Update profile
PUT  /api/users/password       # Change password

# Password Reset
POST /api/auth/forgot-password # Send reset email { email }
POST /api/auth/reset-password  # Reset password { token, newPassword }

# Categories
GET /api/categories            # List product categories

# Reviews (optional)
GET  /api/products/{id}/reviews     # Get product reviews
POST /api/products/{id}/reviews     # Submit review { rating, comment }

# Wishlist (optional)
GET    /api/wishlist           # Get user's wishlist
POST   /api/wishlist           # Add to wishlist { productId }
DELETE /api/wishlist/{id}      # Remove from wishlist

# Contact Form
POST /api/contact              # Submit contact form

# Admin (optional)
GET/POST/PUT/DELETE /api/admin/products
GET/PUT /api/admin/orders
GET /api/admin/users
GET/POST/PUT/DELETE /api/admin/promos
GET /api/admin/dashboard/stats
```

### Guest Session Headers

For unauthenticated users, the app uses guest sessions:
```
Request Headers:
  X-Session-Id: {sessionId}
  X-Session-Secret: {sessionSecret}

Response Headers (on cart operations):
  X-Session-Id: {newSessionId}
  X-Session-Secret: {newSessionSecret}
```

The `guestSessionInterceptor` handles this automatically.

---

## Styling & Design System

### Tailwind Configuration (`tailwind.config.js`)

```javascript
module.exports = {
  content: ["./src/**/*.{html,ts}"],
  theme: {
    extend: {
      colors: {
        primary: {
          50: '#fffbeb', 100: '#fef3c7', 200: '#fde68a', 300: '#fcd34d',
          400: '#fbbf24', 500: '#f59e0b', 600: '#d97706', 700: '#b45309',
          800: '#92400e', 900: '#78350f', 950: '#451a03'
        },
        accent: {
          50: '#fff1f2', 100: '#ffe4e6', 200: '#fecdd3', 300: '#fda4af',
          400: '#fb7185', 500: '#f43f5e', 600: '#e11d48', 700: '#be123c',
          800: '#9f1239', 900: '#881337', 950: '#4c0519'
        },
        brand: {
          50: '#f5f3ff', 100: '#ede9fe', 200: '#ddd6fe', 300: '#c4b5fd',
          400: '#a78bfa', 500: '#8b5cf6', 600: '#7c3aed', 700: '#6d28d9',
          800: '#5b21b6', 900: '#4c1d95', 950: '#2e1065'
        }
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

```css
@tailwind base;
@tailwind components;
@tailwind utilities;

@layer components {
  .btn-primary {
    @apply inline-flex items-center justify-center px-6 py-3
           bg-primary-500 text-white font-semibold rounded-lg
           hover:bg-primary-600 focus:outline-none focus:ring-2
           focus:ring-primary-500 focus:ring-offset-2
           disabled:opacity-50 disabled:cursor-not-allowed
           transition-colors duration-200;
  }

  .btn-secondary {
    @apply inline-flex items-center justify-center px-6 py-3
           bg-slate-200 text-slate-800 font-semibold rounded-lg
           hover:bg-slate-300 transition-colors duration-200;
  }

  .btn-outline {
    @apply inline-flex items-center justify-center px-6 py-3
           border-2 border-primary-500 text-primary-600 font-semibold
           rounded-lg hover:bg-primary-50 transition-colors duration-200;
  }

  .input {
    @apply w-full px-4 py-3 border border-slate-300 rounded-lg
           focus:outline-none focus:ring-2 focus:ring-primary-500
           focus:border-transparent transition-all duration-200;
  }

  .card {
    @apply bg-white rounded-xl shadow-sm border border-slate-100;
  }

  .card-hover {
    @apply card hover:shadow-lg hover:-translate-y-1
           transition-all duration-300;
  }

  .container-wide {
    @apply max-w-7xl mx-auto px-4 sm:px-6 lg:px-8;
  }
}
```

### Common Patterns

**Hero Section:**
```html
<section class="relative bg-gradient-to-br from-slate-900 via-slate-800 to-slate-900 text-white py-24">
  <div class="absolute inset-0 opacity-10">
    <div class="absolute inset-0 hero-pattern"></div>
  </div>
  <div class="relative container-wide">
    <!-- Content -->
  </div>
</section>
```

**Page Header:**
```html
<div class="bg-slate-50 py-12">
  <div class="container-wide">
    <h1 class="text-3xl md:text-4xl font-display font-bold text-slate-800">Page Title</h1>
    <p class="text-slate-600 mt-2">Page description</p>
  </div>
</div>
```

**Grid Layouts:**
```html
<!-- 4-column product grid -->
<div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
  <!-- Product cards -->
</div>

<!-- 3-column feature grid -->
<div class="grid grid-cols-1 md:grid-cols-3 gap-8">
  <!-- Feature cards -->
</div>
```

---

## Testing Strategy

### Unit Testing (Jasmine/Karma)
```bash
npm test          # Run tests
npm run test:ci   # Run tests in CI mode
```

**Key test files to create:**
- `auth.service.spec.ts` - Test login, logout, token refresh
- `cart.service.spec.ts` - Test cart operations
- `product.service.spec.ts` - Test product fetching
- `auth.guard.spec.ts` - Test route protection

### E2E Testing (Playwright or Cypress)
Consider adding for critical flows:
- User registration flow
- Login/logout flow
- Add to cart → checkout flow
- Guest checkout flow

### Manual Testing Checklist

**Authentication:**
- [ ] Register new account
- [ ] Login with valid credentials
- [ ] Login with invalid credentials (error message)
- [ ] Logout clears session
- [ ] Token refresh works on 401
- [ ] Protected routes redirect to login

**Shopping:**
- [ ] Products load on shop page
- [ ] Pagination works
- [ ] Add to cart from product card
- [ ] Add to cart from product detail
- [ ] Update quantity in cart
- [ ] Remove item from cart
- [ ] Apply promo code
- [ ] Remove promo code

**Checkout:**
- [ ] Cart persists across pages
- [ ] Guest checkout works
- [ ] Authenticated checkout works
- [ ] Order confirmation displays correctly
- [ ] Order history shows past orders

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

### Build Commands
```bash
npm run build           # Development build
npm run build -- --configuration=production  # Production build
```

### Docker Support
The API is containerized. Consider adding:
```dockerfile
# sobee_Client/Dockerfile
FROM node:20-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
RUN npm run build -- --configuration=production

FROM nginx:alpine
COPY --from=build /app/dist/sobee-shop/browser /usr/share/nginx/html
COPY nginx.conf /etc/nginx/nginx.conf
EXPOSE 80
```

### CORS Configuration
Ensure API allows requests from frontend domain:
```csharp
// In API Program.cs
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("https://sobee.com", "http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});
```

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

### Feature Pages
| Page | Path |
|------|------|
| Home | `src/app/features/home/` |
| Shop | `src/app/features/shop/` |
| About | `src/app/features/about/` |
| Contact | `src/app/features/contact/` |
| FAQ | `src/app/features/faq/` |
| Checkout | `src/app/features/checkout/` |
| Order Confirmation | `src/app/features/order-confirmation/` |
| Orders | `src/app/features/orders/` |
| Test Page | `src/app/features/test-page/` |

---

## Implementation Priority Order

### Immediate (This Sprint)
1. **Login Page** - Replace test page auth
2. **Register Page** - Complete auth flow
3. **Cart Page** - Dedicated cart management
4. **Product Detail Page** - Full product view

### Next Sprint
5. **User Profile Page** - Account management
6. **Search Functionality** - Navbar search
7. **Category Filtering** - Shop page enhancement
8. **Forgot/Reset Password** - Complete auth

### Future Sprints
9. **Policy Pages** - Static content
10. **Admin Dashboard** - Internal management
11. **Reviews System** - User feedback
12. **Wishlist** - Save for later

---

## Notes for LLM Context

When continuing this project:

1. **Always read files before editing** - The codebase uses standalone components with specific import patterns

2. **Use Angular Signals** - All state management uses signals (`signal()`, `.set()`, `.update()`)

3. **Follow existing patterns** - Check similar components for structure (e.g., look at `home.ts` for page component pattern)

4. **Tailwind classes** - Use existing utility classes from `styles.css` (`.btn-primary`, `.input`, `.card`, etc.)

5. **Service injection** - Use `inject()` function pattern, not constructor injection

6. **API integration** - All HTTP calls go through services, interceptors handle auth/errors automatically

7. **Toast notifications** - Use `ToastService` for user feedback

8. **Route guards** - Update `authGuard` redirect when login page is created

9. **MainLayout wrapper** - All pages should use `<app-main-layout>` for consistent navbar/footer

10. **Build verification** - Run `npm run build` after changes to catch TypeScript errors

---

*Last Updated: January 2026*
*Document Version: 1.0*
