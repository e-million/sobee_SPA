import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { LoginRequest, RegisterRequest, AuthResponse } from '../models';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly apiUrl = environment.apiBaseUrl;

  // Signal to track authentication state
  isAuthenticated = signal(false);

  constructor(private http: HttpClient) {
    // Check if user is already authenticated on service initialization
    const token = localStorage.getItem('accessToken');
    this.isAuthenticated.set(!!token);
  }

  /**
   * Register a new user with profile information
   */
  register(request: RegisterRequest): Observable<any> {
    return this.http.post(`${this.apiUrl}/api/auth/register`, request);
  }

  /**
   * Login with email and password
   */
  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/login`, request).pipe(
      tap(response => {
        // Store tokens in localStorage
        localStorage.setItem('accessToken', response.accessToken);
        localStorage.setItem('refreshToken', response.refreshToken);

        // DON'T clear guest session yet - let the cart service merge first
        // The guest session will be cleared after the first cart request completes

        this.isAuthenticated.set(true);
      })
    );
  }

  /**
   * Clear guest session after cart merge completes
   */
  clearGuestSession(): void {
    localStorage.removeItem('guestSessionId');
    localStorage.removeItem('guestSessionSecret');
  }

  /**
   * Logout the current user
   */
  logout(): void {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    this.isAuthenticated.set(false);
  }

  /**
   * Check if user is authenticated
   */
  isLoggedIn(): boolean {
    return this.isAuthenticated();
  }

  /**
   * Get the current access token
   */
  getToken(): string | null {
    return localStorage.getItem('accessToken');
  }
}
