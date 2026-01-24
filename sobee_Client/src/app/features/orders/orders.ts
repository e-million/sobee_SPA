import { ChangeDetectionStrategy, Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { MainLayout } from '../../shared/layout/main-layout';
import { OrderService } from '../../core/services/order.service';
import { AuthService } from '../../core/services/auth.service';
import { Order } from '../../core/models';

@Component({
  selector: 'app-orders',
  imports: [CommonModule, MainLayout],
  templateUrl: './orders.html',
  styleUrl: './orders.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class Orders implements OnInit {
  orders = signal<Order[]>([]);
  loading = signal(true);
  error = signal('');

  constructor(
    private orderService: OrderService,
    public authService: AuthService,
    private router: Router
  ) {}

  ngOnInit() {
    // Check if user is authenticated
    if (!this.authService.isAuthenticated()) {
      this.router.navigate(['/login']);
      return;
    }

    this.loadOrders();
  }

  loadOrders() {
    this.orderService.getOrders().subscribe({
      next: (orders) => {
        this.orders.set(orders);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(err.message || 'Failed to load orders');
        this.loading.set(false);
      }
    });
  }

  viewOrderDetails(orderId: number) {
    this.router.navigate(['/order-confirmation', orderId]);
  }

  continueShopping() {
    this.router.navigate(['/shop']);
  }

  getStatusClass(status: string | null): string {
    if (!status) return '';

    const statusLower = status.toLowerCase();
    if (statusLower.includes('pending')) return 'bg-amber-100 text-amber-700';
    if (statusLower.includes('processing')) return 'bg-blue-100 text-blue-700';
    if (statusLower.includes('shipped')) return 'bg-sky-100 text-sky-700';
    if (statusLower.includes('delivered')) return 'bg-emerald-100 text-emerald-700';
    if (statusLower.includes('cancelled')) return 'bg-rose-100 text-rose-700';
    return 'bg-slate-100 text-slate-600';
  }

  formatDate(dateString: string | null): string {
    if (!dateString) return 'N/A';

    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }
}
