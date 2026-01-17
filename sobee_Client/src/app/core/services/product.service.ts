import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, of } from 'rxjs';
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
  getProducts(params?: {
    search?: string;
    inStockOnly?: boolean;
    category?: string;
    minPrice?: number;
    maxPrice?: number;
  }): Observable<Product[]> {
    let httpParams = new HttpParams();

    if (params?.search) {
      httpParams = httpParams.set('search', params.search);
    }

    if (params?.inStockOnly !== undefined) {
      httpParams = httpParams.set('inStockOnly', params.inStockOnly.toString());
    }

    if (params?.category) {
      httpParams = httpParams.set('category', params.category);
    }

    if (params?.minPrice !== undefined && params?.minPrice !== null) {
      httpParams = httpParams.set('minPrice', params.minPrice.toString());
    }

    if (params?.maxPrice !== undefined && params?.maxPrice !== null) {
      httpParams = httpParams.set('maxPrice', params.maxPrice.toString());
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
   * Get product categories
   */
  getCategories(): Observable<string[]> {
    return this.http.get<string[]>(`${environment.apiUrl}/categories`);
  }
}
