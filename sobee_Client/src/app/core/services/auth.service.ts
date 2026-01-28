import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, map, of, switchMap, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { LoginRequest, RegisterRequest, AuthResponse, RegisterResponse, ForgotPasswordRequest, ResetPasswordRequest } from '../models';
import { CartService } from './cart.service';

/**
 * Authentication service for login/registration, token storage, and role state.
 */
interface MeResponseClaim {
  type?: string;
  value?: string;
}

interface MeResponse {
  roles?: string[];
  claims?: MeResponseClaim[];
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly apiBaseUrl = environment.apiBaseUrl;
  private readonly apiUrl = `${this.apiBaseUrl}/api`;
  private readonly meUrl = `${this.apiUrl}/me`;
  private readonly rolesStorageKey = 'userRoles';
  private readonly userIdStorageKey = 'userId';

  // Signal to track authentication state
  isAuthenticated = signal(false);
  roles = signal<string[]>([]);
  rolesLoaded = signal(false);
  currentUserId = signal<string | null>(null);

  /**
   * Initializes auth state and cached role data from localStorage.
   * @param http - HttpClient for API calls.
   * @param cartService - CartService for clearing cart on logout.
   */
  constructor(
    private http: HttpClient,
    private cartService: CartService
  ) {
    // Check if user is already authenticated on service initialization
    const token = localStorage.getItem('accessToken');
    this.isAuthenticated.set(!!token);

    const storedRoles = localStorage.getItem(this.rolesStorageKey);
    if (storedRoles) {
      try {
        const parsed = JSON.parse(storedRoles) as string[];
        this.roles.set(parsed);
        this.rolesLoaded.set(true);
      } catch {
        this.roles.set([]);
      }
    }

    const storedUserId = localStorage.getItem(this.userIdStorageKey);
    if (storedUserId) {
      this.currentUserId.set(storedUserId);
    }

    if (token && !this.rolesLoaded()) {
      this.loadRoles().subscribe({ error: () => undefined });
    }
  }

  /**
   * Register a new user and store tokens if the API returns AuthResponse.
   * @param request - Registration payload.
   * @returns Observable of AuthResponse or RegisterResponse from the API.
   */
  register(request: RegisterRequest): Observable<AuthResponse | RegisterResponse> {
    return this.http.post<AuthResponse | RegisterResponse>(`${this.apiUrl}/auth/register`, request).pipe(
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
   * Log in with email and password and hydrate roles on success.
   * @param request - Login credentials.
   * @returns Observable of AuthResponse.
   */
  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiBaseUrl}/login`, request).pipe(
      tap(response => {
        // Store tokens in localStorage
        localStorage.setItem('accessToken', response.accessToken);
        localStorage.setItem('refreshToken', response.refreshToken);

        // DON'T clear guest session yet - let the cart service merge first
        // The guest session will be cleared after the first cart request completes

        this.isAuthenticated.set(true);
      }),
      switchMap(response =>
        this.loadRoles().pipe(
          map(() => response),
          catchError(() => {
            this.rolesLoaded.set(true);
            this.roles.set([]);
            return of(response);
          })
        )
      )
    );
  }

  /**
   * Clear guest session after cart merge completes.
   */
  clearGuestSession(): void {
    localStorage.removeItem('guestSessionId');
    localStorage.removeItem('guestSessionSecret');
  }

  /**
   * Log out the current user and clear tokens, roles, and cart state.
   */
  logout(): void {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    this.clearGuestSession();
    this.cartService.clearCart();
    this.clearRoles();
    this.isAuthenticated.set(false);
  }

  /**
   * Check if the user is authenticated.
   * @returns True if an access token is present in state.
   */
  isLoggedIn(): boolean {
    return this.isAuthenticated();
  }

  /**
   * Get user roles from stored claims.
   * @returns Array of role names.
   */
  getUserRoles(): string[] {
    return this.roles();
  }

  /**
   * Check if the current user has the admin role.
   * @returns True if the user is an admin.
   */
  isAdmin(): boolean {
    return this.getUserRoles().some(role => role.toLowerCase() === 'admin');
  }

  /**
   * Get the current user ID from stored claims.
   * @returns User ID or null if not available.
   */
  getUserId(): string | null {
    return this.currentUserId();
  }

  /**
   * Get the current access token.
   * @returns Access token or null if not present.
   */
  getToken(): string | null {
    return localStorage.getItem('accessToken');
  }

  /**
   * Get the current refresh token.
   * @returns Refresh token or null if not present.
   */
  getRefreshToken(): string | null {
    return localStorage.getItem('refreshToken');
  }

  /**
   * Refresh the access token using the refresh token.
   * @returns Observable of AuthResponse with new tokens.
   */
  refreshToken(): Observable<AuthResponse> {
    const refreshToken = this.getRefreshToken();

    return this.http.post<AuthResponse>(`${this.apiBaseUrl}/refresh`, { refreshToken }).pipe(
      tap(response => {
        localStorage.setItem('accessToken', response.accessToken);
        localStorage.setItem('refreshToken', response.refreshToken);
        this.isAuthenticated.set(true);
      })
    );
  }

  /**
   * Request a password reset email.
   * @param request - Forgot password payload.
   * @returns Observable with success flag.
   */
  forgotPassword(request: ForgotPasswordRequest): Observable<{ success: boolean }> {
    return this.http.post<{ success: boolean }>(`${this.apiUrl}/auth/forgot-password`, request);
  }

  /**
   * Reset password using a token.
   * @param request - Reset password payload.
   * @returns Observable of AuthResponse.
   */
  resetPassword(request: ResetPasswordRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/auth/reset-password`, request);
  }

  /**
   * Load role claims from the API and cache them in localStorage.
   * @returns Observable of role names.
   */
  loadRoles(): Observable<string[]> {
    return this.http.get<MeResponse>(this.meUrl).pipe(
      map(response => {
        const roles = response.roles ?? [];
        const userId = this.extractUserId(response.claims ?? []);
        if (userId) {
          this.currentUserId.set(userId);
          localStorage.setItem(this.userIdStorageKey, userId);
        } else {
          this.currentUserId.set(null);
          localStorage.removeItem(this.userIdStorageKey);
        }
        return roles;
      }),
      tap(roles => {
        this.roles.set(roles);
        this.rolesLoaded.set(true);
        localStorage.setItem(this.rolesStorageKey, JSON.stringify(roles));
      })
    );
  }

  /**
   * Ensure roles are loaded once and return cached values if available.
   * @returns Observable of role names.
   */
  ensureRolesLoaded(): Observable<string[]> {
    if (this.rolesLoaded()) {
      return of(this.roles());
    }

    return this.loadRoles().pipe(
      catchError(() => {
        this.rolesLoaded.set(true);
        this.roles.set([]);
        return of([]);
      })
    );
  }

  /**
   * Clear cached roles and user ID from memory and localStorage.
   */
  clearRoles(): void {
    this.roles.set([]);
    this.rolesLoaded.set(false);
    localStorage.removeItem(this.rolesStorageKey);
    this.currentUserId.set(null);
    localStorage.removeItem(this.userIdStorageKey);
  }

  /**
   * Extract the user ID from JWT claims.
   * @param claims - Claim array from the /me endpoint.
   * @returns User ID or null if missing.
   */
  private extractUserId(claims: MeResponseClaim[]): string | null {
    const idClaim = claims.find(claim =>
      claim.type === 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier' ||
      claim.type === 'sub'
    );

    return idClaim?.value ?? null;
  }

}
