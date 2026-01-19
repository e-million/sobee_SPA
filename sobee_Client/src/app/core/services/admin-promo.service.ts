import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AdminPromo } from '../models';

interface PaginatedResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

@Injectable({
  providedIn: 'root'
})
export class AdminPromoService {
  private readonly apiUrl = `${environment.apiUrl}/admin/promos`;

  constructor(private http: HttpClient) {}

  getPromos(params?: {
    search?: string;
    includeExpired?: boolean;
    page?: number;
    pageSize?: number;
  }): Observable<PaginatedResponse<AdminPromo>> {
    let httpParams = new HttpParams();

    if (params?.search) {
      httpParams = httpParams.set('search', params.search);
    }

    if (params?.includeExpired !== undefined) {
      httpParams = httpParams.set('includeExpired', params.includeExpired.toString());
    }

    if (params?.page) {
      httpParams = httpParams.set('page', params.page.toString());
    }

    if (params?.pageSize) {
      httpParams = httpParams.set('pageSize', params.pageSize.toString());
    }

    return this.http.get<PaginatedResponse<AdminPromo>>(this.apiUrl, { params: httpParams });
  }

  createPromo(payload: {
    code: string;
    discountPercentage: number;
    expirationDate: string;
  }): Observable<AdminPromo> {
    return this.http.post<AdminPromo>(this.apiUrl, payload);
  }

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

  deletePromo(promoId: number): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.apiUrl}/${promoId}`);
  }
}
