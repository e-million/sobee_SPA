import { inject } from '@angular/core';
import { CanActivateChildFn, CanActivateFn, Router } from '@angular/router';
import { map } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { ToastService } from '../services/toast.service';

/**
 * Resolve admin access for both canActivate and canActivateChild.
 * @param stateUrl - URL being accessed.
 * @returns True if access is allowed or an Observable that resolves to true/false.
 */
const canAccessAdmin = (stateUrl: string) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const toastService = inject(ToastService);

  if (!authService.isAuthenticated()) {
    localStorage.setItem('returnUrl', stateUrl);
    router.navigate(['/login']);
    return false;
  }

  return authService.ensureRolesLoaded().pipe(
    map(() => {
      if (!authService.isAdmin()) {
        toastService.error('You do not have access to the admin area.');
        router.navigate(['/']);
        return false;
      }

      return true;
    })
  );
};

/**
 * Guard that blocks non-admin users from admin routes.
 * @param route - Activated route snapshot.
 * @param state - Router state snapshot.
 * @returns True if access is allowed, otherwise false or a redirect.
 */
export const adminGuard: CanActivateFn = (route, state) => {
  return canAccessAdmin(state.url);
};

/**
 * Child-route guard that blocks non-admin users from admin routes.
 * @param route - Activated route snapshot.
 * @param state - Router state snapshot.
 * @returns True if access is allowed, otherwise false or a redirect.
 */
export const adminChildGuard: CanActivateChildFn = (route, state) => {
  return canAccessAdmin(state.url);
};
