# Sobee Angular Rebuild - Feature Roadmap

## Original Application Analysis

The original SobeeYou was an ASP.NET MVC e-commerce application for a non-alcoholic aperitif/energy drink company. This document maps all features from the original to guide the Angular rebuild.

---

## Current Progress (Already Implemented)

- [x] Angular project setup with routing
- [x] Core models (Product, Cart, Order, Auth)
- [x] HTTP interceptors (auth, guest session, error handling, token refresh)
- [x] Auth service (login, register, logout, token refresh)
- [x] Product service with pagination
- [x] Cart service (add, update, remove, promo codes)
- [x] Order service (checkout, order history)
- [x] Test page for API validation
- [x] Checkout page
- [x] Order confirmation page
- [x] Order history page
- [x] Route guards (auth, guest)
- [x] Toast notifications
- [x] Loading spinner component

---

## Feature Roadmap by Priority

### Phase 1: Core Layout & Navigation (HIGH PRIORITY)
*Foundation for all other pages*

| # | Feature | Description | Complexity |
|---|---------|-------------|------------|
| 1.1 | Main Layout | App shell with header, footer, content area | Medium |
| 1.2 | Navbar | Fixed header with logo, nav links (Story, Shop, Account), cart icon with badge | Medium |
| 1.3 | Footer | Dark footer with help links, social links, copyright | Easy |
| 1.4 | Mobile Navigation | Responsive hamburger menu for mobile | Medium |

### Phase 2: Public Pages (HIGH PRIORITY)
*Customer-facing pages*

| # | Feature | Description | Complexity |
|---|---------|-------------|------------|
| 2.1 | Home Page | Hero section, featured products, best sellers, product grid | High |
| 2.2 | Shop/Products Page | Product grid with cards, filtering, sorting, pagination | High |
| 2.3 | Product Card Component | Image, name, price, rating stars, quantity selector, add to cart | Medium |
| 2.4 | About Page | Company story, mission, founders letter | Easy |
| 2.5 | Contact Page | Contact form (name, email, message) | Easy |
| 2.6 | FAQ Page | Accordion-style expandable questions | Medium |

### Phase 3: Authentication Pages (HIGH PRIORITY)
*Replace test page auth forms*

| # | Feature | Description | Complexity |
|---|---------|-------------|------------|
| 3.1 | Login Page | Dedicated login page with styled form | Medium |
| 3.2 | Register Page | Registration form with validation | Medium |
| 3.3 | Forgot Password Page | Email input to initiate password reset | Easy |
| 3.4 | Reset Password Page | New password form with token validation | Medium |
| 3.5 | User Profile/Account Page | View/edit profile, change password, view orders | High |

### Phase 4: Shopping Experience (HIGH PRIORITY)
*Core e-commerce functionality*

| # | Feature | Description | Complexity |
|---|---------|-------------|------------|
| 4.1 | Shopping Cart Page | Full cart view with items, quantities, totals, promo code | High |
| 4.2 | Cart Sidebar/Drawer | Slide-out cart preview (optional) | Medium |
| 4.3 | Checkout Redesign | Multi-step or improved single-page checkout | High |
| 4.4 | Product Detail Page | Full product view with images, description, reviews | High |

### Phase 5: Policy Pages (LOW PRIORITY)
*Legal/informational pages*

| # | Feature | Description | Complexity |
|---|---------|-------------|------------|
| 5.1 | Refund Policy | Static content page | Easy |
| 5.2 | Shipping & Returns | Static content page | Easy |
| 5.3 | Terms of Service | Static content page | Easy |
| 5.4 | Privacy Policy | Static content page | Easy |

### Phase 6: Admin Dashboard (MEDIUM PRIORITY)
*Internal management*

| # | Feature | Description | Complexity |
|---|---------|-------------|------------|
| 6.1 | Admin Layout | Separate layout with sidebar navigation | Medium |
| 6.2 | Dashboard Overview | Stats cards (customers, orders, revenue, products) | High |
| 6.3 | Charts & Analytics | Sales charts, traffic charts using ng2-charts | High |
| 6.4 | Product Management | CRUD for products | High |
| 6.5 | Order Management | View/update orders, change status | High |
| 6.6 | User Management | View users, manage roles | Medium |
| 6.7 | Promo Code Management | Create/manage promotions | Medium |

### Phase 7: Advanced Features (LOW PRIORITY)
*Nice-to-haves*

| # | Feature | Description | Complexity |
|---|---------|-------------|------------|
| 7.1 | Product Reviews | Submit and display reviews with ratings | High |
| 7.2 | Wishlist/Favorites | Save products for later | Medium |
| 7.3 | Product Search | Search by name, category, flavor | Medium |
| 7.4 | Product Filtering | Filter by category, price range, rating | Medium |
| 7.5 | Email Notifications | Order confirmation, shipping updates | Medium |
| 7.6 | Social Media Integration | Instagram feed, share buttons | Medium |

---

## Design System Recommendations

### Color Palette (Modernized from Original)

| Purpose | Original | Recommended Modern |
|---------|----------|-------------------|
| Primary | Orange #ff8c00 | Amber #f59e0b |
| Accent | Hot Pink #ff69b4 | Rose #f43f5e |
| CTA Buttons | Purple #8a2be2 | Violet #8b5cf6 |
| Success | - | Emerald #10b981 |
| Error | - | Red #ef4444 |
| Background | Light Gray #f0f0f0 | Slate #f8fafc |
| Text Primary | Black | Slate #1e293b |
| Text Secondary | - | Slate #64748b |

### Typography
- **Headings**: Inter or Poppins (modern, clean)
- **Body**: Inter or system fonts
- **Weights**: 400 (regular), 500 (medium), 600 (semibold), 700 (bold)

### Component Library Options
1. **Angular Material** - Google's official component library
2. **Tailwind CSS** - Utility-first CSS (recommended for custom designs)
3. **PrimeNG** - Rich component library
4. **Custom CSS** - Full control, more work

### Recommended: Tailwind CSS
- Highly customizable
- Great for unique branding
- Excellent responsive utilities
- Tree-shaking for small bundles
- Modern aesthetic

---

## Page-by-Page Breakdown

### Home Page Components
```
┌─────────────────────────────────────┐
│           NAVBAR                    │
├─────────────────────────────────────┤
│                                     │
│         HERO SECTION                │
│    (Full-width, brand message)      │
│                                     │
├─────────────────────────────────────┤
│      FEATURED PRODUCTS              │
│   ┌─────┐ ┌─────┐ ┌─────┐          │
│   │     │ │     │ │     │          │
│   └─────┘ └─────┘ └─────┘          │
├─────────────────────────────────────┤
│      WHY SOBEE SECTION              │
│   (Benefits, ingredients, etc.)     │
├─────────────────────────────────────┤
│      BEST SELLERS / BUNDLES         │
├─────────────────────────────────────┤
│      TESTIMONIALS (optional)        │
├─────────────────────────────────────┤
│           FOOTER                    │
└─────────────────────────────────────┘
```

### Shop Page Components
```
┌─────────────────────────────────────┐
│           NAVBAR                    │
├─────────────────────────────────────┤
│  PAGE HEADER: "Our Products"        │
├────────┬────────────────────────────┤
│        │                            │
│ FILTER │    PRODUCT GRID            │
│ SIDEBAR│   ┌────┐ ┌────┐ ┌────┐    │
│        │   │    │ │    │ │    │    │
│ □ Cat  │   └────┘ └────┘ └────┘    │
│ □ Cat  │   ┌────┐ ┌────┐ ┌────┐    │
│        │   │    │ │    │ │    │    │
│ Price  │   └────┘ └────┘ └────┘    │
│ ───●── │                            │
│        │      PAGINATION            │
├────────┴────────────────────────────┤
│           FOOTER                    │
└─────────────────────────────────────┘
```

### Cart Page Components
```
┌─────────────────────────────────────┐
│           NAVBAR                    │
├─────────────────────────────────────┤
│  PAGE HEADER: "Shopping Cart"       │
├────────────────────────┬────────────┤
│                        │            │
│  CART ITEMS            │  ORDER     │
│  ┌──────────────────┐  │  SUMMARY   │
│  │ IMG  Name   Qty  │  │            │
│  │      $XX   [+-]  │  │ Subtotal   │
│  │           [X]    │  │ Discount   │
│  └──────────────────┘  │ ────────   │
│  ┌──────────────────┐  │ Total      │
│  │ ...              │  │            │
│  └──────────────────┘  │ [PROMO]    │
│                        │            │
│                        │ [CHECKOUT] │
├────────────────────────┴────────────┤
│           FOOTER                    │
└─────────────────────────────────────┘
```

---

## Suggested Implementation Order

### Sprint 1: Layout Foundation
1. Install Tailwind CSS
2. Create main layout component
3. Build navbar component
4. Build footer component
5. Implement responsive navigation

### Sprint 2: Shop Experience
1. Shop/products page
2. Product card component
3. Product filtering/sorting
4. Pagination component

### Sprint 3: Cart & Checkout
1. Full cart page redesign
2. Improve checkout page
3. Add cart badge to navbar

### Sprint 4: Authentication
1. Login page
2. Register page
3. User profile page

### Sprint 5: Content Pages
1. Home page
2. About page
3. Contact page
4. FAQ page

### Sprint 6: Admin (if needed)
1. Admin layout
2. Dashboard
3. Product management

---

## API Endpoints Required

Based on current API and original features, these endpoints may need to be added:

### Already Available
- POST /api/auth/register
- POST /login
- POST /refresh
- GET /api/products (paginated)
- GET/POST/PUT/DELETE /api/cart
- POST /api/orders/checkout
- GET /api/orders/my
- GET /api/orders/{id}
- GET /api/PaymentMethods

### May Need to Add
- GET /api/products/{id} - Single product details
- GET /api/products/featured - Featured products
- GET /api/categories - Product categories
- GET /api/products?category=X - Filter by category
- POST /api/contact - Contact form submission
- GET/POST /api/reviews - Product reviews
- GET/POST /api/favorites - Wishlist
- PUT /api/users/profile - Update user profile
- POST /api/auth/forgot-password - Initiate password reset
- POST /api/auth/reset-password - Complete password reset
- Admin endpoints for CRUD operations

---

## Next Steps

**Recommended starting point:** Phase 1 (Layout) followed by Phase 2 (Public Pages)

This establishes the visual foundation and gives users something to see immediately. The current test page functionality can then be migrated into proper pages.

Would you like to start with:
1. **Layout & Navigation** - Get the app shell looking professional
2. **Shop/Products Page** - The core shopping experience
3. **Home Page** - The landing page experience

Let me know which to tackle first!
