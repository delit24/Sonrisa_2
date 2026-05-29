import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const adminGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.currentUser()?.role === 'admin') {
    return true;
  }

  // Non-admin trying to access admin page → redirect to dashboard
  router.navigate(['/dashboard']);
  return false;
};
