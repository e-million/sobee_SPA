import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
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
} from '../models';

/**
 * Admin analytics service for dashboard and reporting endpoints.
 */
@Injectable({
  providedIn: 'root'
})
export class AdminService {
  private readonly apiUrl = `${environment.apiUrl}/admin`;
  private readonly analyticsUrl = `${environment.apiUrl}/admin/analytics`;

  constructor(private http: HttpClient) {}

  /**
   * Fetch the high-level admin summary metrics.
   * @returns Observable of AdminSummary.
   */
  getSummary(): Observable<AdminSummary> {
    return this.http.get<AdminSummary>(`${this.apiUrl}/summary`);
  }

  /**
   * Fetch order counts per day for a rolling window.
   * @param days - Number of days to include.
   * @returns Observable of AdminOrdersPerDay[].
   */
  getOrdersPerDay(days: number = 30): Observable<AdminOrdersPerDay[]> {
    const params = new HttpParams().set('days', days.toString());
    return this.http.get<AdminOrdersPerDay[]>(`${this.apiUrl}/orders-per-day`, { params });
  }

  /**
   * Fetch low-stock products below a threshold.
   * @param threshold - Stock threshold.
   * @returns Observable of AdminLowStockProduct[].
   */
  getLowStock(threshold: number = 5): Observable<AdminLowStockProduct[]> {
    const params = new HttpParams().set('threshold', threshold.toString());
    return this.http.get<AdminLowStockProduct[]>(`${this.apiUrl}/low-stock`, { params });
  }

  /**
   * Fetch top-selling products.
   * @param limit - Maximum number of products to return.
   * @returns Observable of AdminTopProduct[].
   */
  getTopProducts(limit: number = 5): Observable<AdminTopProduct[]> {
    const params = new HttpParams().set('limit', limit.toString());
    return this.http.get<AdminTopProduct[]>(`${this.apiUrl}/top-products`, { params });
  }

  /**
   * Fetch revenue metrics for a date range and granularity.
   * @param startDate - Start date ISO string.
   * @param endDate - End date ISO string.
   * @param granularity - Time bucket granularity.
   * @returns Observable of AdminRevenuePoint[].
   */
  getRevenueByPeriod(startDate: string, endDate: string, granularity: string = 'day'): Observable<AdminRevenuePoint[]> {
    const params = new HttpParams()
      .set('startDate', startDate)
      .set('endDate', endDate)
      .set('granularity', granularity);
    return this.http.get<AdminRevenuePoint[]>(`${this.analyticsUrl}/revenue`, { params });
  }

  /**
   * Fetch order status distribution.
   * @returns Observable of AdminOrderStatusBreakdown.
   */
  getOrderStatusBreakdown(): Observable<AdminOrderStatusBreakdown> {
    return this.http.get<AdminOrderStatusBreakdown>(`${this.analyticsUrl}/orders/status`);
  }

  /**
   * Fetch review rating distribution.
   * @returns Observable of AdminRatingDistribution.
   */
  getRatingDistribution(): Observable<AdminRatingDistribution> {
    return this.http.get<AdminRatingDistribution>(`${this.analyticsUrl}/reviews/distribution`);
  }

  /**
   * Fetch most recent reviews.
   * @param limit - Maximum number of reviews to return.
   * @returns Observable of AdminReviewSummary[].
   */
  getRecentReviews(limit: number = 5): Observable<AdminReviewSummary[]> {
    const params = new HttpParams().set('limit', limit.toString());
    return this.http.get<AdminReviewSummary[]>(`${this.analyticsUrl}/reviews/recent`, { params });
  }

  /**
   * Fetch lowest-rated products.
   * @param limit - Maximum number of products to return.
   * @returns Observable of AdminWorstProduct[].
   */
  getWorstProducts(limit: number = 5): Observable<AdminWorstProduct[]> {
    const params = new HttpParams().set('limit', limit.toString());
    return this.http.get<AdminWorstProduct[]>(`${this.analyticsUrl}/products/worst`, { params });
  }

  /**
   * Fetch inventory summary and low-stock counts.
   * @param lowStockThreshold - Stock threshold.
   * @returns Observable of AdminInventorySummary.
   */
  getInventorySummary(lowStockThreshold: number = 5): Observable<AdminInventorySummary> {
    const params = new HttpParams().set('lowStockThreshold', lowStockThreshold.toString());
    return this.http.get<AdminInventorySummary>(`${this.analyticsUrl}/inventory/summary`, { params });
  }

  /**
   * Fetch category performance metrics for a date range.
   * @param startDate - Start date ISO string.
   * @param endDate - End date ISO string.
   * @returns Observable of AdminCategoryPerformance[].
   */
  getCategoryPerformance(startDate: string, endDate: string): Observable<AdminCategoryPerformance[]> {
    const params = new HttpParams()
      .set('startDate', startDate)
      .set('endDate', endDate);
    return this.http.get<AdminCategoryPerformance[]>(`${this.analyticsUrl}/products/categories`, { params });
  }

  /**
   * Fetch fulfillment metrics for a date range.
   * @param startDate - Start date ISO string.
   * @param endDate - End date ISO string.
   * @returns Observable of AdminFulfillmentMetrics.
   */
  getFulfillmentMetrics(startDate: string, endDate: string): Observable<AdminFulfillmentMetrics> {
    const params = new HttpParams()
      .set('startDate', startDate)
      .set('endDate', endDate);
    return this.http.get<AdminFulfillmentMetrics>(`${this.analyticsUrl}/orders/fulfillment`, { params });
  }

  /**
   * Fetch customer breakdown metrics for a date range.
   * @param startDate - Start date ISO string.
   * @param endDate - End date ISO string.
   * @returns Observable of AdminCustomerBreakdown.
   */
  getCustomerBreakdown(startDate: string, endDate: string): Observable<AdminCustomerBreakdown> {
    const params = new HttpParams()
      .set('startDate', startDate)
      .set('endDate', endDate);
    return this.http.get<AdminCustomerBreakdown>(`${this.analyticsUrl}/customers/breakdown`, { params });
  }

  /**
   * Fetch customer growth metrics for a date range.
   * @param startDate - Start date ISO string.
   * @param endDate - End date ISO string.
   * @param granularity - Time bucket granularity.
   * @returns Observable of AdminCustomerGrowthPoint[].
   */
  getCustomerGrowth(startDate: string, endDate: string, granularity: string = 'day'): Observable<AdminCustomerGrowthPoint[]> {
    const params = new HttpParams()
      .set('startDate', startDate)
      .set('endDate', endDate)
      .set('granularity', granularity);
    return this.http.get<AdminCustomerGrowthPoint[]>(`${this.analyticsUrl}/customers/growth`, { params });
  }

  /**
   * Fetch top customers by spend.
   * @param limit - Maximum number of customers to return.
   * @param startDate - Optional start date for filtering.
   * @param endDate - Optional end date for filtering.
   * @returns Observable of AdminTopCustomer[].
   */
  getTopCustomers(limit: number = 5, startDate?: string, endDate?: string): Observable<AdminTopCustomer[]> {
    let params = new HttpParams().set('limit', limit.toString());
    if (startDate) {
      params = params.set('startDate', startDate);
    }
    if (endDate) {
      params = params.set('endDate', endDate);
    }
    return this.http.get<AdminTopCustomer[]>(`${this.analyticsUrl}/customers/top`, { params });
  }

  /**
   * Fetch most wishlisted products.
   * @param limit - Maximum number of products to return.
   * @returns Observable of AdminWishlistProduct[].
   */
  getMostWishlisted(limit: number = 5): Observable<AdminWishlistProduct[]> {
    const params = new HttpParams().set('limit', limit.toString());
    return this.http.get<AdminWishlistProduct[]>(`${this.analyticsUrl}/wishlist/top`, { params });
  }
}
