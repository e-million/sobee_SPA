import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { AuthService } from '../services/auth.service';

/**
 * Guard that redirects unauthenticated users to the login page.
 * @param route - Activated route snapshot.
 * @param state - Router state snapshot.
 * @returns True if authenticated, otherwise false.
 */
export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isAuthenticated()) {
    return true;
  }

  // Store the attempted URL for redirecting after login
  localStorage.setItem('returnUrl', state.url);

  // Redirect to login page
  router.navigate(['/login']);
  return false;
};
