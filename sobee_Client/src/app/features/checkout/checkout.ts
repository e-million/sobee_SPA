import { ChangeDetectionStrategy, Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { MainLayout } from '../../shared/layout/main-layout';
import { CartService } from '../../core/services/cart.service';
import { OrderService } from '../../core/services/order.service';
import { AuthService } from '../../core/services/auth.service';
import { CheckoutRequest, PaymentMethod } from '../../core/models';

@Component({
  selector: 'app-checkout',
  imports: [CommonModule, FormsModule, MainLayout],
  templateUrl: './checkout.html',
  styleUrl: './checkout.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class Checkout implements OnInit {
  loading = signal(false);
  error = signal('');
  paymentMethods = signal<PaymentMethod[]>([]);

  checkoutForm: CheckoutRequest = {
    shippingAddress: '',
    paymentMethodId: null
  };

  constructor(
    public cartService: CartService,
    private orderService: OrderService,
    public authService: AuthService,
    private router: Router
  ) {}

  ngOnInit() {
    // Load payment methods
    this.loadPaymentMethods();

    // Pre-fill shipping address if user is authenticated
    // (Could fetch from user profile in the future)
  }

  loadPaymentMethods() {
    this.orderService.getPaymentMethods().subscribe({
      next: (methods) => {
        this.paymentMethods.set(methods);
        // Auto-select first payment method if available
        if (methods.length > 0 && methods[0].paymentMethodId) {
          this.checkoutForm.paymentMethodId = methods[0].paymentMethodId;
        }
      },
      error: (err) => {
        this.error.set('Failed to load payment methods');
        console.error(err);
      }
    });
  }

  placeOrder() {
    if (!this.checkoutForm.shippingAddress) {
      this.error.set('Shipping address is required');
      return;
    }

    if (!this.checkoutForm.paymentMethodId) {
      this.error.set('Please select a payment method');
      return;
    }

    this.loading.set(true);
    this.error.set('');

    this.orderService.checkout(this.checkoutForm).subscribe({
      next: (order) => {
        this.loading.set(false);
        // Navigate to order confirmation page
        this.router.navigate(['/order-confirmation', order.orderId]);
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err.message || 'Failed to place order');
      }
    });
  }

  goBack() {
    this.router.navigate(['/cart']);
  }
}
