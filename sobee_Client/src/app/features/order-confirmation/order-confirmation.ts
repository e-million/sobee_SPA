import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { OrderService } from '../../core/services/order.service';
import { Order } from '../../core/models';
import { MainLayout } from '../../shared/layout/main-layout';

@Component({
  selector: 'app-order-confirmation',
  imports: [CommonModule, MainLayout],
  templateUrl: './order-confirmation.html',
  styleUrl: './order-confirmation.css',
})
export class OrderConfirmation implements OnInit {
  order = signal<Order | null>(null);
  loading = signal(true);
  error = signal('');

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private orderService: OrderService
  ) {}

  ngOnInit() {
    const orderId = this.route.snapshot.paramMap.get('orderId');

    if (orderId) {
      this.loadOrder(parseInt(orderId, 10));
    } else {
      this.error.set('Order ID not found');
      this.loading.set(false);
    }
  }

  loadOrder(orderId: number) {
    this.orderService.getOrder(orderId).subscribe({
      next: (order) => {
        this.order.set(order);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(err.message || 'Failed to load order');
        this.loading.set(false);
      }
    });
  }

  continueShopping() {
    this.router.navigate(['/test']);
  }

  viewOrders() {
    this.router.navigate(['/orders']);
  }
}
