import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { ProductService } from '../../core/services/product.service';
import { CartService } from '../../core/services/cart.service';
import { Product, LoginRequest, RegisterRequest } from '../../core/models';

@Component({
  selector: 'app-test-page',
  imports: [CommonModule, FormsModule],
  templateUrl: './test-page.html',
  styleUrl: './test-page.css',
})
export class TestPage implements OnInit {
  // Signals for reactive state
  products = signal<Product[]>([]);
  loading = signal(false);
  message = signal('');
  error = signal('');

  // Form models
  loginForm: LoginRequest = { email: '', password: '' };
  registerForm: RegisterRequest = {
    email: '',
    password: '',
    firstName: '',
    lastName: '',
    billingAddress: '',
    shippingAddress: ''
  };

  constructor(
    public authService: AuthService,
    public productService: ProductService,
    public cartService: CartService,
    private router: Router
  ) {}

  ngOnInit() {
    // Load products on initialization
    this.loadProducts();

    // Load cart if not already loaded
    if (!this.cartService.cart()) {
      this.loadCart();
    }
  }

  // === Product Operations ===
  loadProducts() {
    this.loading.set(true);
    this.error.set('');

    this.productService.getProducts().subscribe({
      next: (products) => {
        this.products.set(products);
        this.message.set(`Loaded ${products.length} products`);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(err.message || 'Failed to load products');
        this.loading.set(false);
      }
    });
  }

  // === Cart Operations ===
  loadCart() {
    this.cartService.getCart().subscribe({
      next: (cart) => {
        this.message.set(`Cart loaded: ${cart.items.length} items, Total: $${cart.total}`);

        // If user is authenticated and we just loaded the cart, clear guest session
        // (This ensures cart merge happens before guest session is cleared)
        if (this.authService.isAuthenticated() && localStorage.getItem('guestSessionId')) {
          this.authService.clearGuestSession();
        }
      },
      error: (err) => {
        this.error.set(err.message || 'Failed to load cart');
      }
    });
  }

  addToCart(productId: number) {
    this.cartService.addItem({ productId, quantity: 1 }).subscribe({
      next: (cart) => {
        this.message.set(`Added to cart! Total items: ${cart.items.length}`);
      },
      error: (err) => {
        this.error.set(err.message || 'Failed to add to cart');
      }
    });
  }

  removeFromCart(cartItemId: number) {
    this.cartService.removeItem(cartItemId).subscribe({
      next: (cart) => {
        this.message.set(`Removed from cart! Total items: ${cart.items.length}`);
      },
      error: (err) => {
        this.error.set(err.message || 'Failed to remove from cart');
      }
    });
  }

  // === Auth Operations ===
  login() {
    if (!this.loginForm.email || !this.loginForm.password) {
      this.error.set('Email and password are required');
      return;
    }

    this.loading.set(true);
    this.error.set('');

    this.authService.login(this.loginForm).subscribe({
      next: (response) => {
        this.message.set('Login successful!');
        this.loading.set(false);
        this.loginForm = { email: '', password: '' };

        // Reload cart after login to get user's cart
        this.loadCart();
      },
      error: (err) => {
        this.error.set(err.message || 'Login failed');
        this.loading.set(false);
      }
    });
  }

  register() {
    if (!this.registerForm.email || !this.registerForm.password) {
      this.error.set('Email and password are required');
      return;
    }

    this.loading.set(true);
    this.error.set('');

    this.authService.register(this.registerForm).subscribe({
      next: () => {
        this.message.set('Registration successful! You can now login.');
        this.loading.set(false);
        this.registerForm = {
          email: '',
          password: '',
          firstName: '',
          lastName: '',
          billingAddress: '',
          shippingAddress: ''
        };
      },
      error: (err) => {
        this.error.set(err.message || 'Registration failed');
        this.loading.set(false);
      }
    });
  }

  logout() {
    this.authService.logout();
    this.message.set('Logged out successfully');

    // Reload cart to get guest cart
    this.loadCart();
  }

  clearMessages() {
    this.message.set('');
    this.error.set('');
  }

  goToCheckout() {
    this.router.navigate(['/checkout']);
  }

  goToOrders() {
    this.router.navigate(['/orders']);
  }
}
