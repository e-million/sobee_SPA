import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { PaginatedResponse, Product } from '../models';
import { buildHttpParams } from '../utils/http-params.util';

/**
 * Product service for catalog queries and category lookup.
 */
@Injectable({
  providedIn: 'root'
})
export class ProductService {
  private readonly apiUrl = `${environment.apiUrl}/products`;

  constructor(private http: HttpClient) {}

  /**
   * Get all products with optional filtering.
   * @param params - Query filters for search, stock, category, and price.
   * @returns Observable of Product[].
   */
  getProducts(params?: {
    search?: string;
    inStockOnly?: boolean;
    category?: string;
    minPrice?: number;
    maxPrice?: number;
  }): Observable<Product[]> {
    const httpParams = buildHttpParams({
      search: params?.search,
      inStockOnly: params?.inStockOnly,
      category: params?.category,
      minPrice: params?.minPrice,
      maxPrice: params?.maxPrice
    });

    return this.http.get<PaginatedResponse<Product>>(this.apiUrl, { params: httpParams })
      .pipe(map(response => response.items));
  }

  /**
   * Get a single product by ID.
   * @param id - Product identifier.
   * @returns Observable of Product.
   */
  getProduct(id: number): Observable<Product> {
    return this.http.get<Product>(`${this.apiUrl}/${id}`);
  }

  /**
   * Search products by name or description with client-side fallback.
   * @param searchTerm - User-entered search term.
   * @returns Observable of Product[].
   */
  searchProducts(searchTerm: string): Observable<Product[]> {
    const term = searchTerm.trim().toLowerCase();
    if (!term) {
      return of([]);
    }

    return this.getProducts({ search: searchTerm }).pipe(
      map(products => products.filter(product => {
        const name = product.name?.toLowerCase() ?? '';
        const description = product.description?.toLowerCase() ?? '';
        return name.includes(term) || description.includes(term);
      }))
    );
  }

  /**
   * Get product categories.
   * @returns Observable of category strings.
   */
  getCategories(): Observable<string[]> {
    return this.http.get<string[]>(`${environment.apiUrl}/categories`);
  }
}
