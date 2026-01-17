import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AdminService } from '../../../core/services/admin.service';
import { forkJoin } from 'rxjs';
import { AdminLowStockProduct, AdminOrdersPerDay, AdminSummary, AdminTopProduct } from '../../../core/models';

@Component({
  selector: 'app-admin-dashboard',
  imports: [CommonModule],
  templateUrl: './admin-dashboard.html',
  styleUrl: './admin-dashboard.css'
})
export class AdminDashboard implements OnInit {
  loading = signal(true);
  error = signal('');
  summary = signal<AdminSummary | null>(null);
  ordersPerDay = signal<AdminOrdersPerDay[]>([]);
  lowStock = signal<AdminLowStockProduct[]>([]);
  topProducts = signal<AdminTopProduct[]>([]);

  constructor(private adminService: AdminService) {}

  ngOnInit() {
    this.loadDashboard();
  }

  loadDashboard() {
    this.loading.set(true);
    this.error.set('');

    forkJoin({
      summary: this.adminService.getSummary(),
      orders: this.adminService.getOrdersPerDay(14),
      lowStock: this.adminService.getLowStock(5),
      topProducts: this.adminService.getTopProducts(5)
    }).subscribe({
      next: (response) => {
        this.summary.set(response.summary);
        this.ordersPerDay.set(response.orders);
        this.lowStock.set(response.lowStock);
        this.topProducts.set(response.topProducts);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load the admin dashboard.');
        this.loading.set(false);
      }
    });
  }

  getMaxOrderCount(): number {
    const data = this.ordersPerDay();
    return data.length === 0 ? 0 : Math.max(...data.map(item => item.count));
  }

  getOrderBarWidth(count: number): number {
    const max = this.getMaxOrderCount();
    if (max === 0) {
      return 0;
    }
    return Math.round((count / max) * 100);
  }

  formatDate(dateString: string): string {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
  }
}
