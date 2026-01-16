import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { Product } from '../models';

// Interface for paginated API response
interface PaginatedResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

@Injectable({
  providedIn: 'root'
})
export class ProductService {
  private readonly apiUrl = `${environment.apiUrl}/products`;

  constructor(private http: HttpClient) {}

  /**
   * Get all products with optional filtering
   */
  getProducts(params?: { search?: string; inStockOnly?: boolean; page?: number; pageSize?: number }): Observable<Product[]>;
  getProducts(page?: number, pageSize?: number): Observable<Product[]>;
  getProducts(
    paramsOrPage?: { search?: string; inStockOnly?: boolean; page?: number; pageSize?: number } | number,
    pageSize?: number
  ): Observable<Product[]> {
    let httpParams = new HttpParams();
    let params: { search?: string; inStockOnly?: boolean; page?: number; pageSize?: number } = {};

    if (typeof paramsOrPage === 'number' || paramsOrPage === undefined) {
      if (typeof paramsOrPage === 'number') {
        params.page = paramsOrPage;
      }
      if (typeof pageSize === 'number') {
        params.pageSize = pageSize;
      }
    } else {
      params = paramsOrPage;
    }

    if (params.search) {
      httpParams = httpParams.set('search', params.search);
    }

    if (params.inStockOnly !== undefined) {
      httpParams = httpParams.set('inStockOnly', params.inStockOnly.toString());
    }

    if (params.page !== undefined) {
      httpParams = httpParams.set('page', params.page.toString());
    }

    if (params.pageSize !== undefined) {
      httpParams = httpParams.set('pageSize', params.pageSize.toString());
    }

    return this.http.get<PaginatedResponse<Product>>(this.apiUrl, { params: httpParams })
      .pipe(map(response => response.items));
  }

  /**
   * Get a single product by ID
   */
  getProduct(id: number): Observable<Product> {
    return this.http.get<Product>(`${this.apiUrl}/${id}`);
  }

  /**
   * Search products by name or description
   */
  searchProducts(searchTerm: string): Observable<Product[]> {
    return this.getProducts({ search: searchTerm });
  }
}
