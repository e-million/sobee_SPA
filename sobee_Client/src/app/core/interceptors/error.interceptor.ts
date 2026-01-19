import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { catchError, throwError, retry, timer } from 'rxjs';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { ToastService } from '../services/toast.service';

const RETRY_COUNT = 2;
const RETRY_DELAY = 1000;
const SAFE_METHODS = new Set(['GET', 'HEAD', 'OPTIONS']);

// Endpoints that should not trigger toasts (handled by components)
const SILENT_ENDPOINTS = ['/login', '/register', '/refresh'];

// Status codes that should trigger retry
const RETRYABLE_STATUS_CODES = [408, 500, 502, 503, 504];

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);
  const toastService = inject(ToastService);

  const shouldRetry = SAFE_METHODS.has(req.method);

  return next(req).pipe(
    // Retry for transient errors (idempotent requests only)
    retry({
      count: shouldRetry ? RETRY_COUNT : 0,
      delay: (error: HttpErrorResponse, retryCount: number) => {
        if (RETRYABLE_STATUS_CODES.includes(error.status)) {
          return timer(RETRY_DELAY * retryCount);
        }
        return throwError(() => error);
      }
    }),
    catchError((error: HttpErrorResponse) => {
      let errorMessage = 'An unknown error occurred';
      let showToast = !SILENT_ENDPOINTS.some(ep => req.url.includes(ep));

      if (error.error instanceof ErrorEvent) {
        // Client-side error
        errorMessage = `Error: ${error.error.message}`;
      } else {
        // Server-side error
        switch (error.status) {
          case 0:
            errorMessage = 'Unable to connect to server. Please check your internet connection.';
            break;
          case 401:
            // Unauthorized - don't show toast (handled by token refresh interceptor)
            showToast = false;
            errorMessage = 'Unauthorized. Please log in.';
            break;
          case 403:
            errorMessage = 'You do not have permission to perform this action.';
            break;
          case 404:
            errorMessage = error.error?.message || 'The requested resource was not found.';
            break;
          case 409:
            errorMessage = error.error?.message || 'A conflict occurred with the current state.';
            break;
          case 422:
            errorMessage = error.error?.message || 'Validation failed. Please check your input.';
            break;
          case 429:
            errorMessage = error.error?.message || 'Too many requests. Please try again later.';
            break;
          case 500:
            errorMessage = 'An internal server error occurred. Please try again later.';
            break;
          default:
            if (error.error?.message) {
              errorMessage = error.error.message;
            } else if (error.error?.error) {
              errorMessage = error.error.error;
            } else {
              errorMessage = `Server Error: ${error.status}`;
            }
        }
      }

      console.error('HTTP Error:', errorMessage, error);

      // Show toast notification for user-facing errors
      if (showToast) {
        toastService.error(errorMessage);
      }

      return throwError(() => ({ message: errorMessage, originalError: error }));
    })
  );
};
