# Sobee Angular Client - Testing Guide

This guide will help you test the Angular client integration with your Sobee API.

## Prerequisites

Before testing, ensure:

1. **API is running** - Your Sobee API should be running on `https://localhost:7058`
2. **Database is seeded** - Make sure your database has some products
3. **CORS is configured** - The API already has CORS configured for `http://localhost:4200`

## Starting the Application

1. Open a terminal in the `sobee_Client` folder
2. Run the development server:
   ```bash
   ng serve
   ```
3. Open your browser and navigate to: `http://localhost:4200`

The application will automatically redirect to the test page at `http://localhost:4200/test`

## What You'll See

The test page provides a comprehensive interface to test all major API integrations:

### 1. Authentication Status
- Shows whether you're logged in or using a guest session
- Located at the top of the page

### 2. Login/Register Forms
- **Login**: Test user authentication
  - Email and password fields
  - On successful login, the JWT token is stored in localStorage

- **Register**: Create a new user account
  - Email, password, name, and address fields
  - On successful registration, you can log in with those credentials

- **Logout**: Clear authentication and return to guest mode

### 3. Shopping Cart
- Displays your current cart (guest or authenticated)
- Shows:
  - Cart ID and owner type
  - List of items with product names, prices, quantities
  - Subtotal, discount (if promo applied), and total
  - Remove button for each item
- **Refresh Cart** button to reload cart data

### 4. Products
- Displays all available products in a grid
- Each product card shows:
  - Product image (or placeholder if none)
  - Name and description
  - Price
  - Stock status
  - **Add to Cart** button (disabled if out of stock)

## Testing Scenarios

### Scenario 1: Guest Shopping Experience

1. **Start fresh** - Open the page (you'll be a guest user)
2. **View products** - Products should load automatically
3. **Add to cart** - Click "Add to Cart" on any in-stock product
4. **Check cart** - Cart section should update showing the item
5. **Check localStorage** - Open browser DevTools → Application → Local Storage
   - You should see `guestSessionId` and `guestSessionSecret` headers

### Scenario 2: User Registration and Login

1. **Register a new user**
   - Fill in the registration form
   - Click "Register"
   - You should see a success message

2. **Login with new credentials**
   - Fill in the login form with your new credentials
   - Click "Login"
   - Authentication status should change to "Logged In"
   - Guest session headers should be cleared from localStorage
   - You should see `accessToken` and `refreshToken` in localStorage

3. **Guest cart migration**
   - If you added items as a guest before logging in
   - After login, check if your cart is preserved (API should handle this)

### Scenario 3: Cart Operations

1. **Add multiple products** - Add several different products to cart
2. **View cart items** - Cart table should show all items
3. **Remove an item** - Click "Remove" on any cart item
4. **Verify totals** - Subtotal and total should update correctly

### Scenario 4: Authentication Persistence

1. **Login** - Login with valid credentials
2. **Refresh the page** - Press F5
3. **Verify** - You should still be logged in (token persists in localStorage)

### Scenario 5: Logout Flow

1. **While logged in** - Click the "Logout" button
2. **Verify** - Authentication status changes to "Guest"
3. **Check localStorage** - `accessToken` and `refreshToken` should be cleared
4. **Cart behavior** - A new guest cart should be created

## Testing API Error Handling

### Test Rate Limiting
1. **Rapid requests** - Click "Refresh Products" or "Refresh Cart" many times quickly
2. **Expected**: You should see an error message about rate limiting (429 status)

### Test Invalid Login
1. **Wrong credentials** - Try logging in with incorrect email/password
2. **Expected**: Error message displayed

### Test Unauthorized Access
1. **While logged out** - Try to access a protected endpoint
2. **Expected**: Appropriate error handling

## Browser DevTools Inspection

### Network Tab
1. Open DevTools (F12) → Network tab
2. Perform actions (login, add to cart, etc.)
3. **Inspect requests**:
   - Check if `Authorization: Bearer <token>` header is sent for authenticated requests
   - Check if `X-Guest-Session-Id` and `X-Guest-Session-Secret` are sent for guest requests
   - View request/response payloads

### Console Tab
- Check for any JavaScript errors
- HTTP errors are logged to console

### Application Tab
- **Local Storage** → `http://localhost:4200`
  - `accessToken` - JWT access token (when logged in)
  - `refreshToken` - JWT refresh token (when logged in)
  - `guestSessionId` - Guest session ID (when not logged in)
  - `guestSessionSecret` - Guest session secret (when not logged in)

## Common Issues and Solutions

### Issue: Products not loading
- **Check**: Is the API running on `https://localhost:7058`?
- **Check**: Are there products in your database?
- **Check**: Browser console for CORS errors

### Issue: CORS errors
- **Solution**: Ensure your API's CORS policy includes `http://localhost:4200`
- The API should already have this configured in Program.cs

### Issue: Guest session not working
- **Check**: Network tab to see if session headers are being returned from API
- **Check**: The interceptor is properly configured in app.config.ts

### Issue: Login not working
- **Check**: Is the user registered in the database?
- **Check**: Password meets requirements (min 6 characters)
- **Check**: Network tab to see the actual error response

## What Gets Tested

✅ **HTTP Interceptors**:
- Guest session headers automatically added/captured
- JWT Bearer token automatically added to requests
- Error handling and user-friendly error messages

✅ **Services**:
- AuthService (login, register, logout, token management)
- ProductService (get products)
- CartService (get cart, add/remove items)

✅ **Models**:
- TypeScript interfaces match API DTOs
- Proper type safety throughout the application

✅ **State Management**:
- Angular signals for reactive state
- Cart updates in real-time
- Authentication state persists across page refreshes

## Next Steps After Testing

Once you've verified everything works:

1. **Build real features** - Create proper product listing, cart, and checkout pages
2. **Add routing guards** - Protect authenticated routes
3. **Add more services** - Orders, Favorites, Reviews
4. **Improve UI/UX** - Better styling, loading states, error handling
5. **Add form validation** - Client-side validation for forms

## Need Help?

If you encounter issues:
1. Check the browser console for errors
2. Check the network tab for failed requests
3. Verify the API is running and accessible
4. Check that the API URL in `environments/environment.ts` is correct
