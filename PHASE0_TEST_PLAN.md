# Phase 0 Test Plan (Baseline Behavior Lock)

Purpose
Lock current behavior before refactoring to layered architecture. These tests are intended to run against the existing code and capture the current API contracts and edge cases.

Scope and Order
1) Cart + Orders/Checkout (highest risk)
2) Products
3) Smoke suite (one happy-path endpoint per controller)

Assumptions
- Tests use WebApplicationFactory with a real SQL Server container (Testcontainers) or a dedicated test database.
- Seed data includes products, promos, and users with known roles.
- Identity endpoints are available via MapIdentityApi.

Test Setup (Baseline)
- Seed products:
  - In-stock product (stock >= 10, price > 0)
  - Low-stock product (stock = 1)
  - Out-of-stock product (stock = 0)
- Seed promo:
  - Active promo (not expired, discount > 0)
  - Expired promo
- Seed users:
  - Admin user
  - Standard user
- Seed payment methods:
  - At least 1 valid payment method

Seed References (Use in Tests)
- ProductInStock: stock >= 10, price > 0
- ProductLowStock: stock = 1
- ProductOutOfStock: stock = 0
- PromoActive: not expired, discount > 0
- PromoExpired: expiration date in the past
- UserAdmin: Admin role
- UserStandard: User role
- PaymentMethodDefault: valid payment method id

Acceptance Criteria
- All Phase 0 tests pass without modifying production code.
- Test data setup is deterministic and reusable across test runs.
- Each endpoint has at least 1 happy-path and 1 critical edge-case test.
- Response shapes match PHASE0_RESPONSE_SCHEMAS.md.

1) Cart + Orders/Checkout Baseline

Cart Endpoints
- GET /api/cart
  - Guest: returns cart; creates guest session headers if missing.
  - Authenticated: returns user cart; merges guest cart if session headers present.
- POST /api/cart/items
  - Adds new item.
  - Increments existing item.
  - Fails when requested quantity exceeds stock.
- PUT /api/cart/items/{id}
  - Updates quantity.
  - Quantity = 0 removes item.
  - Fails when requested quantity exceeds stock.
- DELETE /api/cart/items/{id}
  - Removes item.
  - NotFound when item does not exist.
- DELETE /api/cart
  - Clears cart items.
- POST /api/cart/promo/apply
  - Applies valid promo.
  - Rejects expired/invalid promo.
  - Rejects duplicate promo application.
- DELETE /api/cart/promo
  - Removes applied promo.
  - Rejects when no promo exists.

Orders/Checkout Endpoints
- GET /api/orders/{id}
  - Owner can access (user or guest session).
  - Non-owner gets NotFound.
- GET /api/orders/my
  - Auth required.
  - Pagination headers set (X-Total-Count, X-Page, X-Page-Size).
- POST /api/orders/checkout
  - Requires cart with items.
  - Applies promo snapshot (if promo applied).
  - Fails when stock is insufficient.
  - Clears cart on success.
- POST /api/orders/{id}/cancel
  - Cancels when status is cancellable.
  - Rejects invalid status transitions.
- POST /api/orders/{id}/pay
  - Requires valid payment method.
  - Transitions status to Paid when allowed.
  - Rejects invalid transitions.
- PATCH /api/orders/{id}/status (admin)
  - Valid transitions update status and timestamps.
  - Invalid transitions return Conflict.

Detailed Test Cases (Cart + Orders)

Cart Tests
| ID | Scenario | Precondition | Request | Expected |
| --- | --- | --- | --- | --- |
| CART-001 | Guest GetCart creates session | No session headers | GET /api/cart | 200; response has session headers; empty items |
| CART-002 | Guest AddItem new | ProductInStock | POST /api/cart/items { productId, quantity=1 } | 200; item added |
| CART-003 | Guest AddItem increments | Existing cart item | POST /api/cart/items { same productId, quantity=2 } | 200; quantity increased |
| CART-004 | AddItem exceeds stock | ProductLowStock, quantity=2 | POST /api/cart/items | 409; error code InsufficientStock |
| CART-005 | UpdateItem to 0 removes | Existing cart item | PUT /api/cart/items/{id} { quantity=0 } | 200; item removed |
| CART-006 | UpdateItem exceeds stock | ProductLowStock | PUT /api/cart/items/{id} { quantity=2 } | 409; error code InsufficientStock |
| CART-007 | ApplyPromo valid | PromoActive | POST /api/cart/promo/apply { promoCode } | 200; discount percentage returned |
| CART-008 | ApplyPromo expired | PromoExpired | POST /api/cart/promo/apply | 400; error code InvalidPromo |
| CART-009 | ApplyPromo duplicate | PromoActive already applied | POST /api/cart/promo/apply | 409; conflict |
| CART-010 | RemovePromo none | No promo applied | DELETE /api/cart/promo | 400; validation error |
| CART-011 | Auth GetCart merges guest | Guest cart exists + auth headers | GET /api/cart | 200; guest items merged into user cart |

Orders/Checkout Tests
| ID | Scenario | Precondition | Request | Expected |
| --- | --- | --- | --- | --- |
| ORDER-001 | Checkout empty cart | Empty cart | POST /api/orders/checkout | 400; validation error |
| ORDER-002 | Checkout success | Cart has items; PromoActive applied | POST /api/orders/checkout | 200; order created; cart cleared |
| ORDER-003 | Checkout insufficient stock | ProductLowStock with qty=2 | POST /api/orders/checkout | 409; no stock decremented |
| ORDER-004 | GetOrder owner | Order belongs to user/guest | GET /api/orders/{id} | 200; order returned |
| ORDER-005 | GetOrder non-owner | Order belongs to another user | GET /api/orders/{id} | 404; not found |
| ORDER-006 | GetMyOrders pagination | Auth user | GET /api/orders/my?page=1&pageSize=20 | 200; headers X-Total-Count, X-Page, X-Page-Size |
| ORDER-007 | Cancel invalid status | Order already shipped | POST /api/orders/{id}/cancel | 409; invalid status transition |
| ORDER-008 | Pay invalid method | Invalid payment method id | POST /api/orders/{id}/pay | 404; payment method not found |
| ORDER-009 | Pay valid | Pending order, valid payment method | POST /api/orders/{id}/pay | 200; status Paid |
| ORDER-010 | Admin update status | Admin user | PATCH /api/orders/{id}/status { status=Shipped } | 200; shipped date set |

Suggested Test Files
- sobee_API.Tests/Integration/CartEndpointTests.cs
- sobee_API.Tests/Integration/OrdersEndpointTests.cs
- sobee_API.Tests/Integration/CheckoutEndpointTests.cs

2) Products Baseline

Endpoints
- GET /api/products
  - Pagination, search, category filter, sort.
  - Admin sees cost/stock; non-admin does not.
- GET /api/products/{id}
  - Returns product + images.
  - NotFound for missing product.
- POST /api/products (admin)
  - Creates product.
  - Non-admin forbidden.
- PUT /api/products/{id} (admin)
  - Updates subset of fields.
  - Non-admin forbidden.
- DELETE /api/products/{id} (admin)
  - Deletes product and images.
  - Non-admin forbidden.
- POST /api/products/{id}/images (admin)
  - Adds image.
- DELETE /api/products/{productId}/images/{imageId} (admin)
  - Deletes image.

Detailed Test Cases (Products)
| ID | Scenario | Precondition | Request | Expected |
| --- | --- | --- | --- | --- |
| PROD-001 | List products (paging) | Seeded products | GET /api/products?page=1&pageSize=20 | 200; items count <= pageSize |
| PROD-002 | List products (search) | Product name contains term | GET /api/products?q=term | 200; only matching items |
| PROD-003 | List products (category) | Product has category | GET /api/products?category=Name | 200; items in category |
| PROD-004 | List products (sort) | Multiple products | GET /api/products?sort=priceAsc | 200; ascending prices |
| PROD-005 | Admin sees cost/stock | UserAdmin auth | GET /api/products | 200; cost/stock present |
| PROD-006 | Non-admin hides cost/stock | UserStandard auth or anonymous | GET /api/products | 200; cost/stock absent |
| PROD-007 | Get product not found | Missing id | GET /api/products/{id} | 404 |
| PROD-008 | Create product | UserAdmin auth | POST /api/products | 201; new id returned |
| PROD-009 | Update product partial | UserAdmin auth | PUT /api/products/{id} | 200; fields updated |
| PROD-010 | Delete product | UserAdmin auth | DELETE /api/products/{id} | 200; product removed |
| PROD-011 | Add product image | UserAdmin auth | POST /api/products/{id}/images | 200; image id returned |
| PROD-012 | Delete product image | UserAdmin auth | DELETE /api/products/{id}/images/{imageId} | 200; image removed |

Suggested Test Files
- sobee_API.Tests/Integration/ProductsEndpointTests.cs

3) Smoke Suite (Cross-cutting)

Goal
- One happy-path request per controller to detect routing/auth regressions.

Targets (minimum)
- AuthController: POST /api/auth/register (happy path).
- UsersController: GET /api/users/profile (auth).
- MeController: GET /api/me (auth).
- PaymentMethodsController: GET /api/paymentmethods (auth).
- ReviewsController: GET /api/reviews/product/{id}.
- FavoritesController: GET /api/favorites (auth).
- AdminAnalyticsController: GET /api/admin/analytics/inventory/summary (admin).
- AdminPromosController: GET /api/admin/promos (admin).
- AdminUsersController: GET /api/admin/users (admin).

Smoke Suite Cases
| ID | Endpoint | Auth | Expected |
| --- | --- | --- | --- |
| SMOKE-001 | POST /api/auth/register | none | 201 |
| SMOKE-002 | GET /api/users/profile | user | 200 |
| SMOKE-003 | GET /api/me | user | 200 |
| SMOKE-004 | GET /api/paymentmethods | user | 200 |
| SMOKE-005 | GET /api/reviews/product/{id} | none | 200 |
| SMOKE-006 | GET /api/favorites | user | 200 |
| SMOKE-007 | GET /api/admin/analytics/inventory/summary | admin | 200 |
| SMOKE-008 | GET /api/admin/promos | admin | 200 |
| SMOKE-009 | GET /api/admin/users | admin | 200 |

Suggested Test Files
- sobee_API.Tests/Integration/SmokeEndpointTests.cs

Exit Criteria for Phase 0
- All Cart + Orders/Checkout tests pass.
- All Products tests pass.
- Smoke suite passes end-to-end.
