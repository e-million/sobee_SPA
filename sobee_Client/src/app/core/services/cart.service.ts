import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, switchMap, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Cart, AddCartItemRequest, UpdateCartItemRequest, ApplyPromoRequest } from '../models';

@Injectable({
  providedIn: 'root'
})
export class CartService {
  private readonly apiUrl = `${environment.apiUrl}/cart`;

  // Signal to hold current cart state
  cart = signal<Cart | null>(null);

  constructor(private http: HttpClient) {}

  /**
   * Get the current user's cart (or guest cart)
   */
  getCart(): Observable<Cart> {
    return this.http.get<Cart>(this.apiUrl).pipe(
      tap(cart => this.cart.set(cart))
    );
  }

  /**
   * Add an item to the cart
   */
  addItem(request: AddCartItemRequest): Observable<Cart> {
    return this.http.post<Cart>(`${this.apiUrl}/items`, request).pipe(
      tap(cart => this.cart.set(cart))
    );
  }

  /**
   * Update cart item quantity
   */
  updateItem(cartItemId: number, request: UpdateCartItemRequest): Observable<Cart> {
    return this.http.put<Cart>(`${this.apiUrl}/items/${cartItemId}`, request).pipe(
      tap(cart => this.cart.set(cart))
    );
  }

  /**
   * Remove an item from the cart
   */
  removeItem(cartItemId: number): Observable<Cart> {
    return this.http.delete<Cart>(`${this.apiUrl}/items/${cartItemId}`).pipe(
      tap(cart => this.cart.set(cart))
    );
  }

  /**
   * Apply a promo code to the cart
   */
  applyPromo(request: ApplyPromoRequest): Observable<Cart> {
    return this.http.post<void>(`${this.apiUrl}/promo/apply`, request).pipe(
      switchMap(() => this.getCart())
    );
  }

  /**
   * Remove promo code from cart
   */
  removePromo(): Observable<Cart> {
    return this.http.delete<void>(`${this.apiUrl}/promo`).pipe(
      switchMap(() => this.getCart())
    );
  }

  clearCart(): void {
    this.cart.set(null);
  }

  /**
   * Get cart item count
   */
  getItemCount(): number {
    const currentCart = this.cart();
    return currentCart?.items.reduce((sum, item) => sum + (item.quantity || 0), 0) || 0;
  }
}
