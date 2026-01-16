# Sobee Angular Client - Project Structure

## Overview
This Angular application is the client-side UI for the Sobee e-commerce API.

## Folder Structure

```
src/app/
├── core/                      # Core singleton services, guards, and interceptors
│   ├── guards/               # Route guards (auth, role-based access)
│   ├── interceptors/         # HTTP interceptors (auth token, guest session, error handling)
│   ├── models/               # TypeScript interfaces/models matching API DTOs
│   └── services/             # Core services (auth, guest session, etc.)
│
├── features/                 # Feature modules (lazy-loaded)
│   ├── auth/                # Authentication (login, register)
│   ├── cart/                # Shopping cart
│   ├── favorites/           # User favorites/wishlist
│   ├── orders/              # Order history and checkout
│   ├── products/            # Product listing and details
│   └── reviews/             # Product reviews
│
└── shared/                   # Shared components, pipes, and directives
    ├── components/          # Reusable UI components
    ├── directives/          # Custom directives
    └── pipes/               # Custom pipes
```

## Environment Configuration

- **environment.ts** - Default environment (development)
- **environment.development.ts** - Development-specific settings
- **environment.production.ts** - Production settings

Current API endpoint: `https://localhost:7213/api`

## API Integration Features

### Authentication
- JWT Bearer token authentication
- Guest session support (for anonymous users)
- Custom headers: `X-Guest-Session-Id`, `X-Guest-Session-Secret`

### Available API Endpoints
- `/api/auth` - User registration, login
- `/api/products` - Product catalog
- `/api/cart` - Shopping cart management
- `/api/orders` - Order placement and history
- `/api/favorites` - User favorites
- `/api/reviews` - Product reviews
- `/api/payment-methods` - Payment methods
- `/api/admin` - Admin operations

## Next Steps

1. Create TypeScript models matching API DTOs
2. Build HTTP interceptors for:
   - JWT token injection
   - Guest session management
   - Global error handling
3. Create core services (AuthService, CartService, etc.)
4. Build feature components
5. Set up routing and navigation

## Development Server

Run `ng serve` for a dev server. Navigate to `http://localhost:4200/`. The application will automatically reload if you change any of the source files.

## Build

Run `ng build` to build the project. The build artifacts will be stored in the `dist/` directory.
