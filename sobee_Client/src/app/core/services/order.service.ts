import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Order, CheckoutRequest, PaymentMethod } from '../models';

@Injectable({
  providedIn: 'root'
})
export class OrderService {
  private readonly apiUrl = `${environment.apiUrl}/orders`;
  private readonly paymentMethodsUrl = `${environment.apiUrl}/PaymentMethods`;

  // Signal to hold current orders
  orders = signal<Order[]>([]);
  currentOrder = signal<Order | null>(null);

  constructor(private http: HttpClient) {}

  /**
   * Get all orders for the current user (requires authentication)
   */
  getOrders(): Observable<Order[]> {
    return this.http.get<Order[]>(`${this.apiUrl}/my`).pipe(
      tap(orders => this.orders.set(orders))
    );
  }

  /**
   * Get a specific order by ID
   */
  getOrder(orderId: number): Observable<Order> {
    return this.http.get<Order>(`${this.apiUrl}/${orderId}`).pipe(
      tap(order => this.currentOrder.set(order))
    );
  }

  /**
   * Checkout - create an order from the current cart
   */
  checkout(request: CheckoutRequest): Observable<Order> {
    return this.http.post<Order>(`${this.apiUrl}/checkout`, request).pipe(
      tap(order => this.currentOrder.set(order))
    );
  }

  /**
   * Pay for an existing order
   */
  payOrder(orderId: number, paymentMethodId: number): Observable<Order> {
    return this.http.post<Order>(`${this.apiUrl}/${orderId}/pay`, { paymentMethodId }).pipe(
      tap(order => this.currentOrder.set(order))
    );
  }

  /**
   * Get available payment methods
   */
  getPaymentMethods(): Observable<PaymentMethod[]> {
    return this.http.get<PaymentMethod[]>(this.paymentMethodsUrl);
  }

  /**
   * Clear current order from state
   */
  clearCurrentOrder(): void {
    this.currentOrder.set(null);
  }
}
