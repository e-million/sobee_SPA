import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';

/**
 * Interceptor that attaches the Bearer token to outgoing requests.
 * @param req - Outgoing HTTP request.
 * @param next - Next handler in the interceptor chain.
 * @returns Observable of the HTTP event stream.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  // Get the auth token from localStorage
  const token = localStorage.getItem('accessToken');

  // Clone the request and add the Authorization header if token exists
  if (token) {
    req = req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`
      }
    });
  }

  return next(req);
};
