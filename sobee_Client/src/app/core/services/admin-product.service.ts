import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AdminProduct, CreateAdminProductRequest, UpdateAdminProductRequest } from '../models';

interface PaginatedResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

@Injectable({
  providedIn: 'root'
})
export class AdminProductService {
  private readonly apiUrl = `${environment.apiUrl}/products`;

  constructor(private http: HttpClient) {}

  getProducts(params?: {
    search?: string;
    page?: number;
    pageSize?: number;
    sort?: 'priceAsc' | 'priceDesc';
  }): Observable<PaginatedResponse<AdminProduct>> {
    let httpParams = new HttpParams();

    if (params?.search) {
      httpParams = httpParams.set('q', params.search);
    }

    if (params?.page) {
      httpParams = httpParams.set('page', params.page.toString());
    }

    if (params?.pageSize) {
      httpParams = httpParams.set('pageSize', params.pageSize.toString());
    }

    if (params?.sort) {
      httpParams = httpParams.set('sort', params.sort);
    }

    return this.http.get<PaginatedResponse<AdminProduct>>(this.apiUrl, { params: httpParams });
  }

  createProduct(request: CreateAdminProductRequest): Observable<AdminProduct> {
    return this.http.post<AdminProduct>(this.apiUrl, request);
  }

  updateProduct(id: number, request: UpdateAdminProductRequest): Observable<AdminProduct> {
    return this.http.put<AdminProduct>(`${this.apiUrl}/${id}`, request);
  }

  deleteProduct(id: number): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.apiUrl}/${id}`);
  }
}
