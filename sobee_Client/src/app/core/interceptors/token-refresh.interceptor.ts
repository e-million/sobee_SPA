import { HttpInterceptorFn, HttpErrorResponse, HttpRequest, HttpHandlerFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError, BehaviorSubject, filter, take } from 'rxjs';
import { AuthService } from '../services/auth.service';

let isRefreshing = false;
const refreshTokenSubject = new BehaviorSubject<string | null>(null);

/**
 * Interceptor that attempts token refresh on 401 responses.
 * @param req - Outgoing HTTP request.
 * @param next - Next handler in the interceptor chain.
 * @returns Observable of the HTTP event stream.
 */
export const tokenRefreshInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      // Only attempt refresh for 401 errors on non-auth endpoints
      if (error.status === 401 && !isAuthEndpoint(req.url)) {
        return handle401Error(req, next, authService);
      }

      return throwError(() => error);
    })
  );
};

/**
 * Check whether the request is an auth endpoint.
 * @param url - Request URL.
 * @returns True if the URL points to an auth endpoint.
 */
function isAuthEndpoint(url: string): boolean {
  return url.includes('/login') || url.includes('/register') || url.includes('/refresh');
}

/**
 * Handle 401 responses by refreshing the token and retrying the request.
 * @param req - Original HTTP request.
 * @param next - Next handler in the interceptor chain.
 * @param authService - AuthService for refresh/logout.
 * @returns Observable of the retried HTTP request.
 */
function handle401Error(req: HttpRequest<unknown>, next: HttpHandlerFn, authService: AuthService) {
  if (!isRefreshing) {
    isRefreshing = true;
    refreshTokenSubject.next(null);

    const refreshToken = localStorage.getItem('refreshToken');

    if (refreshToken) {
      return authService.refreshToken().pipe(
        switchMap((response) => {
          isRefreshing = false;
          refreshTokenSubject.next(response.accessToken);

          // Retry the original request with new token
          return next(addTokenToRequest(req, response.accessToken));
        }),
        catchError((refreshError) => {
          isRefreshing = false;
          authService.logout();
          return throwError(() => refreshError);
        })
      );
    } else {
      isRefreshing = false;
      authService.logout();
      return throwError(() => new Error('No refresh token available'));
    }
  }

  // Wait for token refresh to complete, then retry
  return refreshTokenSubject.pipe(
    filter((token) => token !== null),
    take(1),
    switchMap((token) => next(addTokenToRequest(req, token!)))
  );
}

/**
 * Clone a request with an Authorization header.
 * @param req - Original HTTP request.
 * @param token - Access token to attach.
 * @returns Cloned HTTP request with Authorization header.
 */
function addTokenToRequest(req: HttpRequest<unknown>, token: string): HttpRequest<unknown> {
  return req.clone({
    setHeaders: {
      Authorization: `Bearer ${token}`
    }
  });
}
