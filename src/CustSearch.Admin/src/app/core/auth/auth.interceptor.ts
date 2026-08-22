import { HttpContextToken, HttpErrorResponse, HttpEvent, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, catchError, switchMap, throwError } from 'rxjs';
import { AuthRefreshService } from './auth-refresh.service';
import { AuthSessionService } from './auth-session.service';

const AUTH_RETRIED = new HttpContextToken<boolean>(() => false);
const REFRESH_AHEAD_SECONDS = 30;

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  if (/\/api\/auth\/(login|refresh|logout)(?:[/?]|$)/.test(request.url)) return next(request);

  const session = inject(AuthSessionService);
  const refresh = inject(AuthRefreshService);
  const router = inject(Router);

  const withToken = (source: HttpRequest<unknown>, token: string | null): HttpRequest<unknown> =>
    token ? source.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : source;

  const retryAfterRefresh = (): Observable<HttpEvent<unknown>> => refresh.refresh().pipe(
    switchMap(token => next(withToken(request.clone({ context: request.context.set(AUTH_RETRIED, true) }), token))),
    catchError(error => {
      if (error instanceof HttpErrorResponse && error.status === 403) {
        void router.navigateByUrl('/access-denied');
      }
      if (error instanceof HttpErrorResponse && error.status === 401) refresh.endSession();
      return throwError(() => error);
    }),
  );

  if (!request.context.get(AUTH_RETRIED) && session.isExpiringWithin(REFRESH_AHEAD_SECONDS)) {
    return retryAfterRefresh();
  }

  return next(withToken(request, session.accessToken())).pipe(
    catchError(error => {
      if (error instanceof HttpErrorResponse && error.status === 401 && !request.context.get(AUTH_RETRIED)) {
        return retryAfterRefresh();
      }
      if (error instanceof HttpErrorResponse && error.status === 403) {
        void router.navigateByUrl('/access-denied');
      }
      return throwError(() => error);
    }),
  );
};
