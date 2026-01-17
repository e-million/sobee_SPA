import { inject } from '@angular/core';
import { CanActivateChildFn, CanActivateFn, Router } from '@angular/router';
import { map } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { ToastService } from '../services/toast.service';

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

export const adminGuard: CanActivateFn = (route, state) => {
  return canAccessAdmin(state.url);
};

export const adminChildGuard: CanActivateChildFn = (route, state) => {
  return canAccessAdmin(state.url);
};
