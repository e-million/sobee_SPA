import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AdminCategory, CreateAdminCategoryRequest, UpdateAdminCategoryRequest } from '../models';

/**
 * Admin category management service for CRUD operations.
 */
@Injectable({
  providedIn: 'root'
})
export class AdminCategoryService {
  private readonly apiUrl = `${environment.apiUrl}/admin/categories`;

  constructor(private http: HttpClient) {}

  /**
   * Fetch all categories.
   */
  getCategories(): Observable<AdminCategory[]> {
    return this.http.get<AdminCategory[]>(this.apiUrl);
  }

  /**
   * Create a new category.
   */
  createCategory(request: CreateAdminCategoryRequest): Observable<AdminCategory> {
    return this.http.post<AdminCategory>(this.apiUrl, request);
  }

  /**
   * Update a category by ID.
   */
  updateCategory(id: number, request: UpdateAdminCategoryRequest): Observable<AdminCategory> {
    return this.http.put<AdminCategory>(`${this.apiUrl}/${id}`, request);
  }

  /**
   * Delete a category by ID.
   */
  deleteCategory(id: number): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.apiUrl}/${id}`);
  }

  /**
   * Force delete a category and reassign products to Uncategorized.
   */
  forceDeleteCategory(id: number): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.apiUrl}/${id}/force`);
  }
}
