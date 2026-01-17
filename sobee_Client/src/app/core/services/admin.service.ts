import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AdminLowStockProduct, AdminOrdersPerDay, AdminSummary, AdminTopProduct } from '../models';

@Injectable({
  providedIn: 'root'
})
export class AdminService {
  private readonly apiUrl = `${environment.apiUrl}/admin`;

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
}
