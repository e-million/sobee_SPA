import { ChangeDetectionStrategy, Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AdminService } from '../../../core/services/admin.service';
import { forkJoin } from 'rxjs';
import { DateFormatPipe } from '../../../shared/pipes/date-format.pipe';
import { PeriodLabelPipe } from '../../../shared/pipes/period-label.pipe';
import { TrendFormatPipe } from '../../../shared/pipes/trend-format.pipe';
import { TruncatePipe } from '../../../shared/pipes/truncate.pipe';
import { UserIdFormatPipe } from '../../../shared/pipes/user-id-format.pipe';
import { WishlistNamePipe } from '../../../shared/pipes/wishlist-name.pipe';
import {
  AdminCategoryPerformance,
  AdminCustomerBreakdown,
  AdminCustomerGrowthPoint,
  AdminFulfillmentMetrics,
  AdminInventorySummary,
  AdminLowStockProduct,
  AdminOrderStatusBreakdown,
  AdminOrdersPerDay,
  AdminRatingDistribution,
  AdminRevenuePoint,
  AdminReviewSummary,
  AdminSummary,
  AdminTopProduct,
  AdminTopCustomer,
  AdminWishlistProduct,
  AdminWorstProduct
} from '../../../core/models';

@Component({
  selector: 'app-admin-dashboard',
  imports: [
    CommonModule,
    DateFormatPipe,
    PeriodLabelPipe,
    TrendFormatPipe,
    TruncatePipe,
    UserIdFormatPipe,
    WishlistNamePipe
  ],
  templateUrl: './admin-dashboard.html',
  styleUrl: './admin-dashboard.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AdminDashboard implements OnInit {
  loading = signal(true);
  error = signal('');
  summary = signal<AdminSummary | null>(null);
  ordersPerDay = signal<AdminOrdersPerDay[]>([]);
  lowStock = signal<AdminLowStockProduct[]>([]);
  topProducts = signal<AdminTopProduct[]>([]);
  revenueByPeriod = signal<AdminRevenuePoint[]>([]);
  revenueGranularity = signal<'day' | 'week' | 'month'>('day');
  orderStatus = signal<AdminOrderStatusBreakdown | null>(null);
  ratingDistribution = signal<AdminRatingDistribution | null>(null);
  recentReviews = signal<AdminReviewSummary[]>([]);
  worstProducts = signal<AdminWorstProduct[]>([]);
  inventorySummary = signal<AdminInventorySummary | null>(null);
  customerBreakdown = signal<AdminCustomerBreakdown | null>(null);
  categoryPerformance = signal<AdminCategoryPerformance[]>([]);
  fulfillmentMetrics = signal<AdminFulfillmentMetrics | null>(null);
  customerGrowth = signal<AdminCustomerGrowthPoint[]>([]);
  topCustomers = signal<AdminTopCustomer[]>([]);
  mostWishlisted = signal<AdminWishlistProduct[]>([]);

  constructor(private adminService: AdminService) {}

  ngOnInit() {
    this.loadDashboard();
  }

  loadDashboard() {
    this.loading.set(true);
    this.error.set('');
    const range = this.getDateRange(30);

    forkJoin({
      summary: this.adminService.getSummary(),
      orders: this.adminService.getOrdersPerDay(14),
      lowStock: this.adminService.getLowStock(5),
      topProducts: this.adminService.getTopProducts(5),
      revenue: this.adminService.getRevenueByPeriod(range.start, range.end, this.revenueGranularity()),
      orderStatus: this.adminService.getOrderStatusBreakdown(),
      ratingDistribution: this.adminService.getRatingDistribution(),
      recentReviews: this.adminService.getRecentReviews(6),
      worstProducts: this.adminService.getWorstProducts(5),
      inventorySummary: this.adminService.getInventorySummary(5),
      categoryPerformance: this.adminService.getCategoryPerformance(range.start, range.end),
      fulfillmentMetrics: this.adminService.getFulfillmentMetrics(range.start, range.end),
      customerBreakdown: this.adminService.getCustomerBreakdown(range.start, range.end),
      customerGrowth: this.adminService.getCustomerGrowth(range.start, range.end, 'day'),
      topCustomers: this.adminService.getTopCustomers(5, range.start, range.end),
      mostWishlisted: this.adminService.getMostWishlisted(5)
    }).subscribe({
      next: (response) => {
        this.summary.set(response.summary);
        this.ordersPerDay.set(response.orders);
        this.lowStock.set(response.lowStock);
        this.topProducts.set(response.topProducts);
        this.revenueByPeriod.set(response.revenue);
        this.orderStatus.set(response.orderStatus);
        this.ratingDistribution.set(response.ratingDistribution);
        this.recentReviews.set(response.recentReviews);
        this.worstProducts.set(response.worstProducts);
        this.inventorySummary.set(response.inventorySummary);
        this.categoryPerformance.set(response.categoryPerformance);
        this.fulfillmentMetrics.set(response.fulfillmentMetrics);
        this.customerBreakdown.set(response.customerBreakdown);
        this.customerGrowth.set(response.customerGrowth);
        this.topCustomers.set(response.topCustomers);
        this.mostWishlisted.set(response.mostWishlisted);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load the admin dashboard.');
        this.loading.set(false);
      }
    });
  }

  setRevenueGranularity(granularity: 'day' | 'week' | 'month') {
    if (this.revenueGranularity() === granularity) {
      return;
    }
    this.revenueGranularity.set(granularity);
    this.loadDashboard();
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

  getMaxRevenue(): number {
    const data = this.revenueByPeriod();
    return data.length === 0 ? 0 : Math.max(...data.map(item => item.revenue));
  }

  getRevenueBarWidth(revenue: number): number {
    const max = this.getMaxRevenue();
    if (max === 0) {
      return 0;
    }
    return Math.round((revenue / max) * 100);
  }

  getOrderStatusItems(): { label: string; count: number }[] {
    const data = this.orderStatus();
    if (!data) {
      return [];
    }

    const items = [
      { label: 'Pending', count: data.pending },
      { label: 'Paid', count: data.paid },
      { label: 'Processing', count: data.processing },
      { label: 'Shipped', count: data.shipped },
      { label: 'Delivered', count: data.delivered },
      { label: 'Cancelled', count: data.cancelled },
      { label: 'Refunded', count: data.refunded }
    ];

    if (data.other) {
      items.push({ label: 'Other', count: data.other });
    }

    return items;
  }

  getOrderStatusBarWidth(count: number): number {
    const total = this.orderStatus()?.total ?? 0;
    if (total === 0) {
      return 0;
    }
    return Math.round((count / total) * 100);
  }

  getRatingItems(): { label: string; count: number }[] {
    const distribution = this.ratingDistribution()?.distribution;
    if (!distribution) {
      return [];
    }

    return [
      { label: '5 stars', count: distribution.fiveStar },
      { label: '4 stars', count: distribution.fourStar },
      { label: '3 stars', count: distribution.threeStar },
      { label: '2 stars', count: distribution.twoStar },
      { label: '1 star', count: distribution.oneStar }
    ];
  }

  getRatingBarWidth(count: number): number {
    const total = this.ratingDistribution()?.totalReviews ?? 0;
    if (total === 0) {
      return 0;
    }
    return Math.round((count / total) * 100);
  }

  getMaxCategoryRevenue(): number {
    const data = this.categoryPerformance();
    return data.length === 0 ? 0 : Math.max(...data.map(item => item.revenue));
  }

  getCategoryBarWidth(revenue: number): number {
    const max = this.getMaxCategoryRevenue();
    if (max === 0) {
      return 0;
    }
    return Math.round((revenue / max) * 100);
  }

  getMaxNewRegistrations(): number {
    const data = this.customerGrowth();
    return data.length === 0 ? 0 : Math.max(...data.map(item => item.newRegistrations));
  }

  getCustomerGrowthBarWidth(count: number): number {
    const max = this.getMaxNewRegistrations();
    if (max === 0) {
      return 0;
    }
    return Math.round((count / max) * 100);
  }

  getTopCustomerLabel(customer: AdminTopCustomer): string {
    if (customer.name) {
      return customer.name;
    }
    if (customer.email) {
      return customer.email;
    }
    return `User ${customer.userId.slice(0, 8)}`;
  }

  private getDateRange(days: number): { start: string; end: string } {
    const end = new Date();
    const start = new Date();
    start.setDate(end.getDate() - days);

    return {
      start: start.toISOString(),
      end: end.toISOString()
    };
  }
}
