import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { FavoritesResponse } from '../models';

@Injectable({
  providedIn: 'root'
})
export class FavoritesService {
  private readonly apiUrl = `${environment.apiUrl}/favorites`;

  constructor(private http: HttpClient) {}

  getFavorites(page = 1, pageSize = 100): Observable<FavoritesResponse> {
    return this.http.get<FavoritesResponse>(this.apiUrl, {
      params: {
        page,
        pageSize
      }
    });
  }

  addFavorite(productId: number): Observable<unknown> {
    return this.http.post(`${this.apiUrl}/${productId}`, {});
  }

  removeFavorite(productId: number): Observable<unknown> {
    return this.http.delete(`${this.apiUrl}/${productId}`);
  }
}
