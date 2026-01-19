import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { AuthService } from '../services/auth.service';

/**
 * Guard that prevents authenticated users from accessing guest-only routes.
 * @param route - Activated route snapshot.
 * @param state - Router state snapshot.
 * @returns True if not authenticated, otherwise false.
 */
export const guestGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (!authService.isAuthenticated()) {
    return true;
  }

  // Already logged in, redirect to home
  router.navigate(['/']);
  return false;
};
