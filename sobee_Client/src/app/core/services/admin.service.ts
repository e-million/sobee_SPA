import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AdminCustomerBreakdown,
  AdminInventorySummary,
  AdminLowStockProduct,
  AdminOrderStatusBreakdown,
  AdminOrdersPerDay,
  AdminRatingDistribution,
  AdminRevenuePoint,
  AdminReviewSummary,
  AdminSummary,
  AdminTopProduct,
  AdminWorstProduct
} from '../models';

@Injectable({
  providedIn: 'root'
})
export class AdminService {
  private readonly apiUrl = `${environment.apiUrl}/admin`;
  private readonly analyticsUrl = `${environment.apiUrl}/admin/analytics`;

  constructor(private http: HttpClient) {}

  getSummary(): Observable<AdminSummary> {
    return this.http.get<AdminSummary>(`${this.apiUrl}/summary`);
  }

  getOrdersPerDay(days: number = 30): Observable<AdminOrdersPerDay[]> {
    const params = new HttpParams().set('days', days.toString());
    return this.http.get<AdminOrdersPerDay[]>(`${this.apiUrl}/orders-per-day`, { params });
  }

  getLowStock(threshold: number = 5): Observable<AdminLowStockProduct[]> {
    const params = new HttpParams().set('threshold', threshold.toString());
    return this.http.get<AdminLowStockProduct[]>(`${this.apiUrl}/low-stock`, { params });
  }

  getTopProducts(limit: number = 5): Observable<AdminTopProduct[]> {
    const params = new HttpParams().set('limit', limit.toString());
    return this.http.get<AdminTopProduct[]>(`${this.apiUrl}/top-products`, { params });
  }

  getRevenueByPeriod(startDate: string, endDate: string, granularity: string = 'day'): Observable<AdminRevenuePoint[]> {
    const params = new HttpParams()
      .set('startDate', startDate)
      .set('endDate', endDate)
      .set('granularity', granularity);
    return this.http.get<AdminRevenuePoint[]>(`${this.analyticsUrl}/revenue`, { params });
  }

  getOrderStatusBreakdown(): Observable<AdminOrderStatusBreakdown> {
    return this.http.get<AdminOrderStatusBreakdown>(`${this.analyticsUrl}/orders/status`);
  }

  getRatingDistribution(): Observable<AdminRatingDistribution> {
    return this.http.get<AdminRatingDistribution>(`${this.analyticsUrl}/reviews/distribution`);
  }

  getRecentReviews(limit: number = 5): Observable<AdminReviewSummary[]> {
    const params = new HttpParams().set('limit', limit.toString());
    return this.http.get<AdminReviewSummary[]>(`${this.analyticsUrl}/reviews/recent`, { params });
  }

  getWorstProducts(limit: number = 5): Observable<AdminWorstProduct[]> {
    const params = new HttpParams().set('limit', limit.toString());
    return this.http.get<AdminWorstProduct[]>(`${this.analyticsUrl}/products/worst`, { params });
  }

  getInventorySummary(lowStockThreshold: number = 5): Observable<AdminInventorySummary> {
    const params = new HttpParams().set('lowStockThreshold', lowStockThreshold.toString());
    return this.http.get<AdminInventorySummary>(`${this.analyticsUrl}/inventory/summary`, { params });
  }

  getCustomerBreakdown(startDate: string, endDate: string): Observable<AdminCustomerBreakdown> {
    const params = new HttpParams()
      .set('startDate', startDate)
      .set('endDate', endDate);
    return this.http.get<AdminCustomerBreakdown>(`${this.analyticsUrl}/customers/breakdown`, { params });
  }
}
