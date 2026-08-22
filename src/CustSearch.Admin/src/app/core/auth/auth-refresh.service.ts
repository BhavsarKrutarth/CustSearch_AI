import { HttpBackend, HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, catchError, finalize, map, shareReplay, throwError } from 'rxjs';
import { AuthResponse } from './auth.models';
import { AuthSessionService } from './auth-session.service';

@Injectable({ providedIn: 'root' })
export class AuthRefreshService {
  private readonly session = inject(AuthSessionService);
  private readonly router = inject(Router);
  private readonly client = new HttpClient(inject(HttpBackend));
  private inFlight: Observable<string> | null = null;

  refresh(): Observable<string> {
    if (this.inFlight) return this.inFlight;

    const request = this.client.post<AuthResponse>('/api/auth/refresh', {}, { withCredentials: true }).pipe(
      map(response => {
        if (!response.accessToken || !this.session.setAccessToken(response.accessToken)) {
          throw new Error('Refresh returned an invalid or expired access token.');
        }
        if (response.user) this.session.setCurrentUser(response.user);
        return response.accessToken;
      }),
      catchError(error => {
        this.endSession();
        return throwError(() => error);
      }),
      finalize(() => { this.inFlight = null; }),
      shareReplay({ bufferSize: 1, refCount: false }),
    );
    this.inFlight = request;
    return request;
  }

  logout(): Observable<void> {
    return this.client.post<void>('/api/auth/logout', {}, { withCredentials: true }).pipe(
      finalize(() => this.endSession()),
    );
  }

  endSession(): void {
    this.session.clear();
    void this.router.navigateByUrl('/login');
  }
}
