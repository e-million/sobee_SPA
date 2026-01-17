import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { LoginRequest, RegisterRequest, AuthResponse, RegisterResponse, ForgotPasswordRequest, ResetPasswordRequest } from '../models';
import { CartService } from './cart.service';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly apiUrl = environment.apiBaseUrl;

  // Signal to track authentication state
  isAuthenticated = signal(false);

  constructor(
    private http: HttpClient,
    private cartService: CartService
  ) {
    // Check if user is already authenticated on service initialization
    const token = localStorage.getItem('accessToken');
    this.isAuthenticated.set(!!token);
  }

  /**
   * Register a new user with profile information
   */
  register(request: RegisterRequest): Observable<AuthResponse | RegisterResponse> {
    return this.http.post<AuthResponse | RegisterResponse>(`${this.apiUrl}/api/auth/register`, request).pipe(
      tap(response => {
        const accessToken = (response as AuthResponse | null)?.accessToken;
        const refreshToken = (response as AuthResponse | null)?.refreshToken;

        if (accessToken && refreshToken) {
          localStorage.setItem('accessToken', accessToken);
          localStorage.setItem('refreshToken', refreshToken);
          this.isAuthenticated.set(true);
        }
      })
    );
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
    this.clearGuestSession();
    this.cartService.clearCart();
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

  /**
   * Get the current refresh token
   */
  getRefreshToken(): string | null {
    return localStorage.getItem('refreshToken');
  }

  /**
   * Refresh the access token using the refresh token
   */
  refreshToken(): Observable<AuthResponse> {
    const refreshToken = this.getRefreshToken();

    return this.http.post<AuthResponse>(`${this.apiUrl}/refresh`, { refreshToken }).pipe(
      tap(response => {
        localStorage.setItem('accessToken', response.accessToken);
        localStorage.setItem('refreshToken', response.refreshToken);
        this.isAuthenticated.set(true);
      })
    );
  }

  /**
   * Request a password reset email
   */
  forgotPassword(request: ForgotPasswordRequest): Observable<{ success: boolean }> {
    return this.http.post<{ success: boolean }>(`${this.apiUrl}/api/auth/forgot-password`, request);
  }

  /**
   * Reset password using a token
   */
  resetPassword(request: ResetPasswordRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/api/auth/reset-password`, request);
  }
}
