import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { FavoritesResponse } from '../models';

/**
 * Favorites service for managing wishlist items.
 */
@Injectable({
  providedIn: 'root'
})
export class FavoritesService {
  private readonly apiUrl = `${environment.apiUrl}/favorites`;

  constructor(private http: HttpClient) {}

  /**
   * Fetch a paginated list of favorites.
   * @param page - Page number (1-based).
   * @param pageSize - Page size.
   * @returns Observable of FavoritesResponse.
   */
  getFavorites(page = 1, pageSize = 100): Observable<FavoritesResponse> {
    return this.http.get<FavoritesResponse>(this.apiUrl, {
      params: {
        page,
        pageSize
      }
    });
  }

  /**
   * Add a product to favorites.
   * @param productId - Product identifier.
   * @returns Observable from the API.
   */
  addFavorite(productId: number): Observable<{
    message: string;
    favoriteId?: number;
    productId?: number;
    added?: boolean;
  }> {
    return this.http.post<{
      message: string;
      favoriteId?: number;
      productId?: number;
      added?: boolean;
    }>(`${this.apiUrl}/${productId}`, {});
  }

  /**
   * Remove a product from favorites.
   * @param productId - Product identifier.
   * @returns Observable from the API.
   */
  removeFavorite(productId: number): Observable<{ message: string; productId?: number }> {
    return this.http.delete<{ message: string; productId?: number }>(`${this.apiUrl}/${productId}`);
  }
}
