import { HttpClient } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthResponse } from '../../core/auth/auth.models';
import { AuthSessionService } from '../../core/auth/auth-session.service';
import { ThemeService } from '../../core/theme/theme.service';

@Component({
  selector: 'app-login-page',
  imports: [FormsModule],
  templateUrl: './login-page.html',
  styleUrl: './login-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LoginPage {
  private readonly http = inject(HttpClient);
  private readonly session = inject(AuthSessionService);
  private readonly router = inject(Router);
  private readonly theme = inject(ThemeService);
  protected readonly busy = signal(false);
  protected readonly error = signal('');
  protected tenantCode = '';
  protected username = '';
  protected password = '';

  constructor() {
    this.theme.applyContextDefault('light');
  }

  protected signIn(): void {
    if (this.busy()) return;
    this.busy.set(true);
    this.error.set('');
    this.http.post<AuthResponse>('/api/auth/login', {
      tenantCode: this.tenantCode.trim() || null,
      username: this.username.trim(),
      password: this.password,
    }, { withCredentials: true }).pipe(finalize(() => this.busy.set(false))).subscribe({
      next: response => {
        this.password = '';
        if (!this.session.setAccessToken(response.accessToken)) {
          this.error.set('Sign in was unsuccessful. Please try again.');
          return;
        }
        this.session.setCurrentUser(response.user);
        void this.router.navigateByUrl(response.user.isPlatformAdmin ? '/platform-admin' : '/customer-admin');
      },
      error: () => {
        this.password = '';
        this.error.set('Sign in was unsuccessful. Check your details and try again.');
      },
    });
  }
}
