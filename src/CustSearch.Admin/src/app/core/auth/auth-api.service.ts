import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { CurrentSessionResponse } from './auth.models';
import { AuthSessionService } from './auth-session.service';

/** Loads the server-validated current identity and copies only authorization state into memory. */
@Injectable({ providedIn: 'root' })
export class AuthApiService {
  private readonly http = inject(HttpClient);
  private readonly session = inject(AuthSessionService);

  loadCurrentSession(): Observable<CurrentSessionResponse> {
    return this.http.get<CurrentSessionResponse>('/api/auth/me').pipe(
      tap(response => this.session.setCurrentUser(response.user)),
    );
  }

  /** Changes only the authenticated user's credential; successful completion revokes every session. */
  changePassword(currentPassword: string, newPassword: string, confirmNewPassword: string): Observable<void> {
    return this.http.post<void>('/api/auth/change-password', {
      currentPassword,
      newPassword,
      confirmNewPassword,
    }, { withCredentials: true });
  }
}
