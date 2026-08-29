import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { finalize } from 'rxjs';
import { ApiErrorResponse, AuthResponse } from '../../core/auth/auth.models';
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
  private readonly route = inject(ActivatedRoute);
  private readonly theme = inject(ThemeService);
  protected readonly busy = signal(false);
  protected readonly error = signal('');
  protected readonly errorReference = signal('');
  protected readonly showPassword = signal(false);
  protected readonly message = signal(
    this.route.snapshot.queryParamMap.get('passwordChanged') === '1'
      ? 'Password changed successfully. Sign in with your new password.'
      : '');
  protected tenantCode = '';
  protected username = '';
  protected password = '';

  constructor() {
    this.theme.applyContextDefault('light');
  }

  protected signIn(): void {
    if (this.busy()) return;
    if (!this.username.trim() || !this.password) {
      this.error.set('Enter your username and password to continue.');
      return;
    }
    this.busy.set(true);
    this.error.set('');
    this.errorReference.set('');
    this.message.set('');
    this.session.clear();
    this.http.post<AuthResponse>('/api/auth/login', {
      tenantCode: this.tenantCode.trim() || null,
      username: this.username.trim(),
      password: this.password,
    }, { withCredentials: true }).pipe(finalize(() => this.busy.set(false))).subscribe({
      next: response => {
        this.password = '';
        if (!response?.user || typeof response.user.isPlatformAdmin !== 'boolean' || !this.session.setAccessToken(response.accessToken)) {
          this.session.clear();
          this.error.set('The sign-in response was incomplete. Please try again.');
          return;
        }
        this.session.setCurrentUser(response.user);
        void this.router.navigateByUrl(response.user.isPlatformAdmin ? '/platform-admin' : '/customer-admin');
      },
      error: error => {
        this.password = '';
        this.showError(error);
      },
    });
  }

  protected togglePassword(): void { this.showPassword.update(value => !value); }

  private showError(error: unknown): void {
    this.errorReference.set('');
    if (!(error instanceof HttpErrorResponse)) {
      this.error.set('Sign in could not be completed. Try again.');
      return;
    }
    if (error.status === 0) {
      this.error.set('The admin service is unreachable. Start the API or check your connection, then try again.');
      return;
    }
    if (error.status === 429) {
      this.error.set('Too many sign-in attempts. Wait a moment and try again.');
    } else if (error.status === 401) {
      const code = this.apiCode(error);
      this.error.set(code === 'UserDisabled'
        ? 'This account is disabled. Ask an administrator to reactivate it.'
        : code === 'TenantUnavailable'
          ? 'This customer workspace is unavailable. Check the tenant code or contact support.'
          : 'The tenant code, username, or password is not valid. Check the details and try again.');
    } else if (error.status >= 500) {
      this.error.set('The admin service is temporarily unavailable. Try again shortly.');
    } else {
      this.error.set('Sign in could not be completed. Check the highlighted details and try again.');
    }
    const reference = this.apiError(error)?.correlationId;
    if (reference) this.errorReference.set(`Reference: ${reference}`);
  }

  private apiError(error: HttpErrorResponse): ApiErrorResponse | null {
    const body = error.error as Partial<ApiErrorResponse> | null;
    return body && typeof body.correlationId === 'string' ? body as ApiErrorResponse : null;
  }

  private apiCode(error: HttpErrorResponse): string | null { return this.apiError(error)?.code ?? null; }
}
