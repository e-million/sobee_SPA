import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AdminProduct, CreateAdminProductRequest, UpdateAdminProductRequest, PaginatedResponse } from '../models';
import { buildHttpParams } from '../utils/http-params.util';

/**
 * Admin product management service for CRUD operations.
 */
@Injectable({
  providedIn: 'root'
})
export class AdminProductService {
  private readonly apiUrl = `${environment.apiUrl}/products`;

  constructor(private http: HttpClient) {}

  /**
   * Fetch a paginated list of products with optional sorting.
   * @param params - Query options for search, pagination, and sort.
   * @returns Observable of a paginated AdminProduct response.
   */
  getProducts(params?: {
    search?: string;
    category?: string;
    page?: number;
    pageSize?: number;
    sort?: 'priceAsc' | 'priceDesc';
  }): Observable<PaginatedResponse<AdminProduct>> {
    const httpParams = buildHttpParams({
      q: params?.search,
      category: params?.category,
      page: params?.page,
      pageSize: params?.pageSize,
      sort: params?.sort
    });

    return this.http.get<PaginatedResponse<AdminProduct>>(this.apiUrl, { params: httpParams });
  }

  /**
   * Create a new product.
   * @param request - Product creation payload.
   * @returns Observable of the created AdminProduct.
   */
  createProduct(request: CreateAdminProductRequest): Observable<AdminProduct> {
    return this.http.post<AdminProduct>(this.apiUrl, request);
  }

  /**
   * Update an existing product by ID.
   * @param id - Product identifier.
   * @param request - Product update payload.
   * @returns Observable of the updated AdminProduct.
   */
  updateProduct(id: number, request: UpdateAdminProductRequest): Observable<AdminProduct> {
    return this.http.put<AdminProduct>(`${this.apiUrl}/${id}`, request);
  }

  /**
   * Delete a product by ID.
   * @param id - Product identifier.
   * @returns Observable containing a confirmation message.
   */
  deleteProduct(id: number): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.apiUrl}/${id}`);
  }
}
