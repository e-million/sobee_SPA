# Sobee API (Backend)

## 1) Project Overview
Sobee API is an ASP.NET Core e-commerce backend that handles authentication, catalog browsing, cart and checkout flows, and order lifecycle management over a REST-style HTTP API. It also includes operational endpoints for health, metrics, and structured logging to support local development and production monitoring.【F:sobee_API/sobee_API/Program.cs†L35-L656】

Major domains covered:
- Auth (Identity minimal endpoints plus a custom register endpoint at `/api/auth/register`).【F:sobee_API/sobee_API/Program.cs†L598-L600】【F:sobee_API/sobee_API/Controllers/AuthController.cs†L6-L63】
- Products.【F:sobee_API/sobee_API/Controllers/ProductsController.cs†L1-L278】
- Cart (guest + authenticated).【F:sobee_API/sobee_API/Controllers/CartController.cs†L1-L567】【F:sobee_API/sobee_API/Services/GuestSessionService.cs†L1-L108】
- Orders.【F:sobee_API/sobee_API/Controllers/OrdersController.cs†L1-L554】
- Reviews.【F:sobee_API/sobee_API/Controllers/ReviewsController.cs†L1-L213】
- Favorites.【F:sobee_API/sobee_API/Controllers/FavoritesController.cs†L1-L114】
- Admin endpoints (order summaries, low stock, top products).【F:sobee_API/sobee_API/Controllers/AdminController.cs†L1-L126】

## 2) Tech Stack
- .NET 8 / ASP.NET Core (TargetFramework: `net8.0`).【F:sobee_API/sobee_API/sobee_API.csproj†L1-L9】
- EF Core + SQL Server for the application data store.【F:sobee_API/sobee_API/Program.cs†L53-L92】【F:sobee_API/sobee_API/sobee_API.csproj†L14-L24】
- Testing: xUnit + `WebApplicationFactory`, with in-memory SQLite for tests.【F:sobee_API/sobee_API.Tests/sobee_API.Tests.csproj†L1-L20】【F:sobee_API/sobee_API.Tests/TestWebApplicationFactory.cs†L1-L89】
- FluentValidation (request DTO validation).【F:sobee_API/sobee_API/Program.cs†L328-L333】【F:sobee_API/sobee_API/sobee_API.csproj†L11-L12】
- Serilog (console JSON formatting).【F:sobee_API/sobee_API/Program.cs†L35-L49】
- Rate limiting (ASP.NET Core RateLimiter).【F:sobee_API/sobee_API/Program.cs†L158-L307】
- OpenTelemetry metrics + Prometheus scraping endpoint (if enabled) at `/metrics`.【F:sobee_API/sobee_API/Program.cs†L425-L429】【F:sobee_API/sobee_API/Program.cs†L640-L642】
- Swagger/OpenAPI (enabled in Development).【F:sobee_API/sobee_API/Program.cs†L470-L487】

## 3) Repository Layout (Backend)
```
.
├── sobee_API
│   ├── Controllers
│   ├── DTOs
│   │   ├── Auth
│   │   ├── Cart
│   │   ├── Common
│   │   ├── Orders
│   │   ├── Products
│   │   └── Reviews
│   ├── Validation
│   ├── Constants
│   ├── Middleware
│   ├── Services
│   ├── Program.cs
│   └── sobee_API.http
└── sobee_API.Tests
```

Key backend folders:
- `sobee_API/Controllers`: HTTP API controllers for auth, products, cart, orders, reviews, favorites, and admin APIs.【F:sobee_API/sobee_API/Controllers/AdminController.cs†L1-L126】
- `sobee_API/DTOs`: Request/response DTOs organized by functional area (Auth, Cart, Orders, Products, Reviews, Common).【F:sobee_API/sobee_API/DTOs/Common/ApiErrorResponse.cs†L1-L30】
- `sobee_API/Validation`: FluentValidation validators for request DTOs.【F:sobee_API/sobee_API/Validation/AddCartItemRequestValidator.cs†L1-L1】
- `sobee_API/Constants`: Shared constants such as order statuses.【F:sobee_API/sobee_API/Constants/OrderStatuses.cs†L1-L83】
- `sobee_API/Middleware`: Correlation ID and security header middleware.【F:sobee_API/sobee_API/Middleware/CorrelationIdMiddleware.cs†L1-L62】【F:sobee_API/sobee_API/Middleware/SecurityHeadersMiddleware.cs†L1-L39】
- `sobee_API.Tests`: Test project using `WebApplicationFactory` and SQLite in-memory databases.【F:sobee_API/sobee_API.Tests/TestWebApplicationFactory.cs†L1-L104】

## 4) Running the API Locally
1. **Prerequisites**
   - .NET SDK 8 (`net8.0`).【F:sobee_API/sobee_API/sobee_API.csproj†L1-L9】
   - SQL Server accessible via the `Sobee` connection string (local or Docker).【F:sobee_API/sobee_API/Program.cs†L53-L92】
2. **Configure the connection string**
   - Preferred: set `ConnectionStrings__Sobee` to override appsettings.
   - Or edit `sobee_API/sobee_API/appsettings.json` `ConnectionStrings:Sobee` for local use (do not commit secrets).【F:sobee_API/sobee_API/appsettings.json†L1-L6】

   ```bash
   export ConnectionStrings__Sobee="Server=localhost,1433;Database=sobee;User Id=sa;Password=<password>;TrustServerCertificate=True;"
   ```
3. **Restore/build/run**
   ```bash
   dotnet restore
   dotnet build
   dotnet run --project sobee_API/sobee_API
   ```
4. **Swagger/OpenAPI**
   - Enabled in Development at `/swagger`.【F:sobee_API/sobee_API/Program.cs†L470-L487】
5. **Base URLs (dev)**
   - `https://localhost:7058` (HTTPS) or `http://localhost:5029` (HTTP).【F:sobee_API/sobee_API/Properties/launchSettings.json†L11-L27】

## 4.1) Running the API + SQL Server with Docker
This repo ships a `docker-compose.yml` that runs SQL Server and the ASP.NET Core API together. The API still supports the local `https://localhost:7058` workflow while running in Docker.

### One-time HTTPS dev cert setup (host)
```bash
mkdir -p .aspnet/https
dotnet dev-certs https -ep .aspnet/https/aspnetapp.pfx -p "<your-password>"
dotnet dev-certs https --trust
```

### Configure environment variables
Create a local `.env` file from the example (do **not** commit it):
```bash
cp .env.example .env
```
Set values for:
- `SA_PASSWORD`
- `HTTPS_CERT_PASSWORD`
- `SOBEE_DB_NAME` (optional; defaults to `sobee`)

### Build + run containers
```bash
docker compose build
docker compose up -d
```

### Verify API
```bash
curl -k https://localhost:7058/api/home/ping
curl -k https://localhost:7058/health/ready
```

### Database migrations (manual, if needed)
If the database is empty, apply EF Core migrations from your host machine:
```bash
dotnet ef database update --project sobee_API/Sobee.Domain --startup-project sobee_API/sobee_API
```

### Troubleshooting
- **SQL not ready**: wait for the SQL Server container healthcheck to pass, then retry the API.
- **TLS/cert errors**: ensure `.aspnet/https/aspnetapp.pfx` exists and that `HTTPS_CERT_PASSWORD` matches.
- **Port conflicts**: stop any local app using `7058`, `5029`, or `1433`.

## 5) Authentication Model
### 5.1 Identity endpoints (Minimal APIs)
`MapIdentityApi<ApplicationUser>()` exposes the ASP.NET Core Identity minimal API endpoints (rooted at `/`) including:
- `POST /register`
- `POST /login`
- `POST /refresh`
- `POST /confirmEmail`
- `POST /resendConfirmationEmail`
- `POST /forgotPassword`
- `POST /resetPassword`
- `POST /manage/2fa`
- `GET /manage/info`

These endpoints are mapped by the framework when Identity API endpoints are enabled, and tokens are consumed via `Authorization: Bearer <token>` on protected endpoints.【F:sobee_API/sobee_API/Program.cs†L112-L152】【F:sobee_API/sobee_API/Program.cs†L598-L600】

The API also has a **custom registration endpoint** that creates a user profile but does **not** issue a token:
- `POST /api/auth/register`【F:sobee_API/sobee_API/Controllers/AuthController.cs†L6-L63】

### 5.2 Guest Session flow (Cart)
Guest cart operations use session headers managed by the API:
- `X-Session-Id`
- `X-Session-Secret`

If a guest session does not exist, the API can issue both headers in the response when a cart is created or resolved. Subsequent cart requests must include both headers to attach to that session. **Do not log or persist `X-Session-Secret`.**【F:sobee_API/sobee_API/Services/GuestSessionService.cs†L1-L88】

## 6) Standard API Response Contracts
### 6.1 Error contract: ApiErrorResponse
The API uses a common error envelope for many endpoints:
```json
{
  "error": "string",
  "code": "string",
  "details": { /* optional */ }
}
```
【F:sobee_API/sobee_API/DTOs/Common/ApiErrorResponse.cs†L1-L30】

Examples (from cart/order flows):
- **404 NotFound** with details
  ```json
  { "error": "Cart item 123 not found.", "code": "NotFound", "details": { "cartItemId": 123 } }
  ```
  【F:sobee_API/sobee_API/Controllers/CartController.cs†L241-L242】
- **409 Conflict** with details
  ```json
  { "error": "Promo code already applied to this cart.", "code": "Conflict", "details": { "promoCode": "PROMO10" } }
  ```
  【F:sobee_API/sobee_API/Controllers/CartController.cs†L141-L146】
- **400 Validation error**
  ```json
  {
    "error": "Validation failed.",
    "code": "VALIDATION_ERROR",
    "details": { "errors": { "fieldName": ["message"] } }
  }
  ```
  【F:sobee_API/sobee_API/Program.cs†L332-L417】

### 6.2 Validation behavior (FluentValidation + ApiBehaviorOptions)
- FluentValidation is registered for request DTO validation.【F:sobee_API/sobee_API/Program.cs†L328-L333】
- `InvalidModelStateResponseFactory` normalizes `ModelState` keys and returns `ApiErrorResponse` with `VALIDATION_ERROR` code.【F:sobee_API/sobee_API/Program.cs†L332-L417】
- Body-bound errors map to `body`, including binder errors (`""` or `"$"`).【F:sobee_API/sobee_API/Program.cs†L346-L399】
- Route/query parameter errors keep their own keys (camel-cased) and are **not** forced into `body`.【F:sobee_API/sobee_API/Program.cs†L346-L399】

## 7) Rate Limiting & Hardening
Rate limiting is enabled globally with policy selection based on path/method:
- `AuthPolicy` for `/api/auth/*` and `/login`.
- `WritePolicy` for write methods (`POST`, `PUT`, `PATCH`, `DELETE`) on `/api/cart` and `/api/orders`.
- `GlobalPolicy` for all other traffic.

Rate limit partitioning uses the authenticated user id when available, otherwise the caller IP address. (Guest sessions do **not** change the rate-limit partition key.)【F:sobee_API/sobee_API/Program.cs†L158-L307】

429 responses return JSON:
```json
{ "error": "Too many requests.", "code": "RATE_LIMITED" }
```
【F:sobee_API/sobee_API/Program.cs†L289-L307】

Hardening middleware:
- Correlation ID header: `X-Correlation-Id` is accepted and echoed back if absent, or generated per request.【F:sobee_API/sobee_API/Middleware/CorrelationIdMiddleware.cs†L1-L62】
- Security headers middleware sets defaults if absent (e.g., `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy`).【F:sobee_API/sobee_API/Middleware/SecurityHeadersMiddleware.cs†L1-L39】

## 8) Observability
- Serilog writes structured JSON logs to console (compact JSON format).【F:sobee_API/sobee_API/Program.cs†L35-L49】
- Correlation ID is propagated into logs and response headers via middleware.【F:sobee_API/sobee_API/Middleware/CorrelationIdMiddleware.cs†L1-L62】【F:sobee_API/sobee_API/Program.cs†L520-L568】
- Prometheus scraping endpoint: `GET /metrics` (enabled and exempt from rate limiting).【F:sobee_API/sobee_API/Program.cs†L640-L642】
- Health checks:
  - `GET /health/live`
  - `GET /health/ready`
  (both exempt from rate limiting).【F:sobee_API/sobee_API/Program.cs†L628-L638】

## 9) Running Tests
- Run tests:
  ```bash
  dotnet test
  ```
- Tests live in `sobee_API.Tests` and use `WebApplicationFactory` with in-memory SQLite for both core and identity contexts.【F:sobee_API/sobee_API.Tests/TestWebApplicationFactory.cs†L1-L104】
- Be mindful of provider differences: column definitions must stay SQLite-compatible to keep tests passing (avoid SQL Server-only column types in migrations/EF model when tests target SQLite).【F:sobee_API/sobee_API.Tests/TestWebApplicationFactory.cs†L1-L104】

## 10) Manual Smoke Testing
Use the VS Code/Visual Studio HTTP files in `sobee_API/sobee_API`:
- `sobee_API.http`: observability checks, rate-limiting, correlation headers, and core product/cart calls.【F:sobee_API/sobee_API/sobee_API.http†L1-L88】
- `checkout endpoint test.http`: guest cart flow, login, checkout, order lifecycle checks.【F:sobee_API/sobee_API/checkout endpoint test.http†L1-L95】

### How to verify
Open either `.http` file and run the requests in order. These scripts cover:
- auth/register and login
- guest cart creation (session headers)
- promo apply/remove
- checkout + order flow
- validation error examples
- rate limit checks

The exact request sequence and expected outcomes are documented inline in the `.http` files.【F:sobee_API/sobee_API/sobee_API.http†L1-L88】【F:sobee_API/sobee_API/checkout endpoint test.http†L1-L95】

## 11) Common Troubleshooting
- **404 on login**: ensure you are using the minimal Identity endpoint `/login` (not `/identity/login`).【F:sobee_API/sobee_API/Program.cs†L598-L600】【F:sobee_API/sobee_API/Controllers/AuthController.cs†L50-L63】
- **JSON body parse errors**: check `.http` files for malformed JSON or missing commas; the VS HTTP client is strict about JSON formatting.【F:sobee_API/sobee_API/sobee_API.http†L1-L88】
- **Rate limiting appears not to trigger**: limits are per-minute and partition by user id or IP; requests must be rapid and within the same minute to hit the 429 response.【F:sobee_API/sobee_API/Program.cs†L158-L307】
- **SQLite vs SQL Server differences in tests**: test DB is SQLite; avoid SQL Server-only column types or behaviors in EF models when writing tests/migrations.【F:sobee_API/sobee_API.Tests/TestWebApplicationFactory.cs†L1-L104】

## 12) Roadmap (Backend Complete)
- Backend complete through hardening (J6).
- Next: Angular SPA integration.
- Next: Deployment pipeline.
