import { Injectable, inject } from '@angular/core';
import { Observable, catchError, finalize, map, of, shareReplay, switchMap } from 'rxjs';
import { AuthApiService } from './auth-api.service';
import { AuthRefreshService } from './auth-refresh.service';
import { AuthSessionService } from './auth-session.service';

/** Restores an HttpOnly-cookie session once and then loads authoritative roles and permissions from `/me`. */
@Injectable({ providedIn: 'root' })
export class SessionBootstrapService {
  private readonly session = inject(AuthSessionService);
  private readonly refresh = inject(AuthRefreshService);
  private readonly api = inject(AuthApiService);
  private inFlight: Observable<boolean> | null = null;

  ensureSession(): Observable<boolean> {
    if (this.session.isAuthenticated() && this.session.currentUser()) return of(true);
    if (this.inFlight) return this.inFlight;

    const restore = this.session.isAuthenticated()
      ? this.api.loadCurrentSession()
      : this.refresh.refresh().pipe(switchMap(() => this.api.loadCurrentSession()));
    this.inFlight = restore.pipe(
      map(() => true),
      catchError(() => {
        // A failed refresh or `/me` lookup must not leave stale roles or permissions in memory.
        this.session.clear();
        return of(false);
      }),
      finalize(() => { this.inFlight = null; }),
      shareReplay({ bufferSize: 1, refCount: false }),
    );
    return this.inFlight;
  }
}
