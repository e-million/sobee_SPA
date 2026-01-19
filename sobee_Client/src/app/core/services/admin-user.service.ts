import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AdminUser, PaginatedResponse } from '../models';

/**
 * Admin user management service for listing users and role updates.
 */
@Injectable({
  providedIn: 'root'
})
export class AdminUserService {
  private readonly apiUrl = `${environment.apiUrl}/admin/users`;

  constructor(private http: HttpClient) {}

  /**
   * Fetch a paginated list of users with optional search.
   * @param params - Query options for search and pagination.
   * @returns Observable of a paginated AdminUser response.
   */
  getUsers(params?: {
    search?: string;
    page?: number;
    pageSize?: number;
  }): Observable<PaginatedResponse<AdminUser>> {
    let httpParams = new HttpParams();

    if (params?.search) {
      httpParams = httpParams.set('search', params.search);
    }

    if (params?.page) {
      httpParams = httpParams.set('page', params.page.toString());
    }

    if (params?.pageSize) {
      httpParams = httpParams.set('pageSize', params.pageSize.toString());
    }

    return this.http.get<PaginatedResponse<AdminUser>>(this.apiUrl, { params: httpParams });
  }

  /**
   * Grant or revoke admin role for a user.
   * @param userId - User identifier.
   * @param isAdmin - True to grant admin access, false to revoke.
   * @returns Observable of the updated AdminUser.
   */
  setAdmin(userId: string, isAdmin: boolean): Observable<AdminUser> {
    return this.http.put<AdminUser>(`${this.apiUrl}/${userId}/admin`, { isAdmin });
  }
}
