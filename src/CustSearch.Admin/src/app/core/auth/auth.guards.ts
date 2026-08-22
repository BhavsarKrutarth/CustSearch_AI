import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map } from 'rxjs';
import { AuthSessionService } from './auth-session.service';
import { SessionBootstrapService } from './session-bootstrap.service';

/** Restores the cookie session when possible and otherwise sends anonymous users to login. */
export const authGuard: CanActivateFn = () => {
  const bootstrap = inject(SessionBootstrapService);
  const router = inject(Router);
  return bootstrap.ensureSession().pipe(map(ready => ready ? true : router.createUrlTree(['/login'])));
};

/** Allows a route only when the authenticated user has at least one accepted role. */
export const roleGuard = (acceptedRoles: readonly string[]): CanActivateFn => () => {
  const bootstrap = inject(SessionBootstrapService);
  const session = inject(AuthSessionService);
  const router = inject(Router);
  return bootstrap.ensureSession().pipe(map(ready =>
    ready && acceptedRoles.some(role => session.hasRole(role))
      ? true
      : router.createUrlTree(['/access-denied'])));
};

/** Allows a route only when every required granular permission is present in the server session. */
export const permissionGuard = (requiredPermissions: readonly string[]): CanActivateFn => () => {
  const bootstrap = inject(SessionBootstrapService);
  const session = inject(AuthSessionService);
  const router = inject(Router);
  return bootstrap.ensureSession().pipe(map(ready =>
    ready && requiredPermissions.every(permission => session.hasPermission(permission))
      ? true
      : router.createUrlTree(['/access-denied'])));
};
