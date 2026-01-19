import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AdminPromo, PaginatedResponse } from '../models';
import { buildHttpParams } from '../utils/http-params.util';

/**
 * Admin promo management service for listing and editing promo codes.
 */
@Injectable({
  providedIn: 'root'
})
export class AdminPromoService {
  private readonly apiUrl = `${environment.apiUrl}/admin/promos`;

  constructor(private http: HttpClient) {}

  /**
   * Fetch a paginated list of promo codes.
   * @param params - Query options for search, filtering, and pagination.
   * @returns Observable of a paginated AdminPromo response.
   */
  getPromos(params?: {
    search?: string;
    includeExpired?: boolean;
    page?: number;
    pageSize?: number;
  }): Observable<PaginatedResponse<AdminPromo>> {
    const httpParams = buildHttpParams({
      search: params?.search,
      includeExpired: params?.includeExpired,
      page: params?.page,
      pageSize: params?.pageSize
    });

    return this.http.get<PaginatedResponse<AdminPromo>>(this.apiUrl, { params: httpParams });
  }

  /**
   * Create a new promo code.
   * @param payload - Promo code details.
   * @returns Observable of the created AdminPromo.
   */
  createPromo(payload: {
    code: string;
    discountPercentage: number;
    expirationDate: string;
  }): Observable<AdminPromo> {
    return this.http.post<AdminPromo>(this.apiUrl, payload);
  }

  /**
   * Update an existing promo code.
   * @param promoId - Promo identifier.
   * @param payload - Updated fields for the promo.
   * @returns Observable of the updated AdminPromo.
   */
  updatePromo(
    promoId: number,
    payload: {
      code?: string;
      discountPercentage?: number;
      expirationDate?: string;
    }
  ): Observable<AdminPromo> {
    return this.http.put<AdminPromo>(`${this.apiUrl}/${promoId}`, payload);
  }

  /**
   * Delete a promo code by ID.
   * @param promoId - Promo identifier.
   * @returns Observable containing a confirmation message.
   */
  deletePromo(promoId: number): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.apiUrl}/${promoId}`);
  }
}
