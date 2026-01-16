# Quick Start Guide

Get the Sobee Angular client up and running in 3 steps.

## Step 1: Start the API

Make sure your Sobee API is running:

```bash
cd sobee_API/sobee_API
dotnet run
```

The API should start on: `https://localhost:7058`

## Step 2: Start the Angular App

In a new terminal:

```bash
cd sobee_Client
ng serve
```

The app will start on: `http://localhost:4200`

## Step 3: Open in Browser

Navigate to: **http://localhost:4200**

You'll automatically be redirected to the test page.

## What You Can Test

### As a Guest (No Login Required)
1. ✅ Browse products
2. ✅ Add items to cart
3. ✅ View cart totals
4. ✅ Remove items from cart

### As a Registered User
1. ✅ Register a new account
2. ✅ Login with credentials
3. ✅ All guest features
4. ✅ Cart persists across sessions

## Quick Test Scenario

1. **Add a product to cart** (as guest)
   - Click "Add to Cart" on any product
   - Cart section updates automatically

2. **Register an account**
   - Fill in registration form
   - Click "Register"

3. **Login**
   - Use credentials from step 2
   - Your guest cart should migrate to your user account

4. **Verify in Browser DevTools**
   - Open DevTools (F12)
   - Go to Application → Local Storage
   - See `accessToken` and `refreshToken`

## Troubleshooting

**Products not loading?**
- Check API is running on https://localhost:7058
- Check browser console for errors

**CORS errors?**
- API CORS is already configured for localhost:4200
- Restart both API and Angular if needed

**Build errors?**
- Run `npm install` to ensure all dependencies are installed

## Next Steps

Once everything works, read:
- [TESTING_GUIDE.md](TESTING_GUIDE.md) - Comprehensive testing instructions
- [IMPLEMENTATION_SUMMARY.md](IMPLEMENTATION_SUMMARY.md) - Architecture overview
- [PROJECT_STRUCTURE.md](PROJECT_STRUCTURE.md) - Folder structure

Happy coding! 🚀
