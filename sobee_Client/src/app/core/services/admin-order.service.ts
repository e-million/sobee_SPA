import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Order, UpdateOrderStatusRequest } from '../models';

/**
 * Admin order management service for updating order status.
 */
@Injectable({
  providedIn: 'root'
})
export class AdminOrderService {
  private readonly apiUrl = `${environment.apiUrl}/orders`;

  constructor(private http: HttpClient) {}

  /**
   * Update an order's status.
   * @param orderId - Order identifier.
   * @param request - Status update payload.
   * @returns Observable of the updated Order.
   */
  updateOrderStatus(orderId: number, request: UpdateOrderStatusRequest): Observable<Order> {
    return this.http.patch<Order>(`${this.apiUrl}/${orderId}/status`, request);
  }
}
