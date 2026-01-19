import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminService } from '../../../core/services/admin.service';
import { Order } from '../../../core/models';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-admin-orders',
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-orders.html',
  styleUrl: './admin-orders.css'
})
export class AdminOrders {
  orderId = '';
  status = 'Processing';
  updating = signal(false);
  error = signal('');
  updatedOrder = signal<Order | null>(null);

  statusOptions = [
    'Pending',
    'Paid',
    'Processing',
    'Shipped',
    'Delivered',
    'Cancelled'
  ];

  constructor(
    private adminService: AdminService,
    private toastService: ToastService
  ) {}

  updateStatus() {
    const orderIdNumber = Number(this.orderId);
    if (!orderIdNumber || Number.isNaN(orderIdNumber)) {
      this.error.set('Enter a valid order ID.');
      return;
    }

    this.updating.set(true);
    this.error.set('');

    this.adminService.updateOrderStatus(orderIdNumber, { status: this.status }).subscribe({
      next: (order) => {
        this.updatedOrder.set(order);
        this.toastService.success('Order status updated.');
        this.updating.set(false);
      },
      error: () => {
        this.error.set('Failed to update order status.');
        this.updating.set(false);
      }
    });
  }
}
