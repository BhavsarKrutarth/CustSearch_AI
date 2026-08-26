import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthApiService } from '../../core/auth/auth-api.service';
import { AuthSessionService } from '../../core/auth/auth-session.service';
import { AdminShell } from '../../shared/admin-shell/admin-shell';

const STRONG_PASSWORD = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{10,500}$/;

@Component({
  selector: 'app-change-password-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, AdminShell],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styles: [`
    :host{display:block}.password-card{background:var(--color-surface);border:1px solid var(--color-border);border-radius:var(--radius-md);max-width:38rem;padding:clamp(1.25rem,4vw,2rem)}
    form{display:grid;gap:1rem}label{display:grid;font-size:.78rem;font-weight:700;gap:.4rem}input{background:var(--color-bg);border:1px solid var(--color-border);border-radius:var(--radius-sm);color:var(--color-text);min-height:2.8rem;padding:.65rem .75rem}
    button{background:var(--color-accent);border:0;border-radius:var(--radius-sm);color:var(--color-on-accent);cursor:pointer;font-weight:700;min-height:2.8rem}button:disabled{cursor:not-allowed;opacity:.55}.hint{color:var(--color-muted);font-size:.75rem;line-height:1.5}.error{color:var(--color-danger)}
  `],
  template: `
    <app-admin-shell [adminType]="adminType()" pageTitle="Change password" eyebrow="Account security">
      <section class="password-card">
        <p class="hint">Use at least 10 characters with upper-case, lower-case and a number. After a successful change, every active session is revoked and you must sign in again.</p>
        @if(error()){<p class="error" role="alert">{{error()}}</p>}
        <form [formGroup]="form" (ngSubmit)="submit()">
          <label>Current password<input type="password" formControlName="currentPassword" autocomplete="current-password"></label>
          <label>New password<input type="password" formControlName="newPassword" autocomplete="new-password"></label>
          <label>Confirm new password<input type="password" formControlName="confirmNewPassword" autocomplete="new-password"></label>
          <button [disabled]="form.invalid||busy()">{{busy()?'Changing password…':'Change password and sign out'}}</button>
        </form>
      </section>
    </app-admin-shell>
  `,
})
export class ChangePasswordPage {
  private readonly api = inject(AuthApiService);
  private readonly session = inject(AuthSessionService);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  protected readonly busy = signal(false);
  protected readonly error = signal('');
  protected readonly adminType = computed<'customer'|'platform'>(() =>
    this.session.currentUser()?.isPlatformAdmin ? 'platform' : 'customer');
  protected readonly form = this.fb.nonNullable.group({
    currentPassword: ['', [Validators.required, Validators.maxLength(500)]],
    newPassword: ['', [Validators.required, Validators.pattern(STRONG_PASSWORD)]],
    confirmNewPassword: ['', [Validators.required, Validators.pattern(STRONG_PASSWORD)]],
  });

  protected submit(): void {
    if (this.form.invalid || this.busy()) return;
    const value = this.form.getRawValue();
    if (value.newPassword !== value.confirmNewPassword) {
      this.error.set('New password and confirmation do not match.');
      return;
    }
    if (value.currentPassword === value.newPassword) {
      this.error.set('New password must be different from the current password.');
      return;
    }
    this.busy.set(true);
    this.error.set('');
    this.api.changePassword(value.currentPassword, value.newPassword, value.confirmNewPassword)
      .pipe(finalize(() => this.busy.set(false)))
      .subscribe({
        next: () => {
          this.form.reset();
          this.session.clear();
          void this.router.navigate(['/login'], { queryParams: { passwordChanged: '1' } });
        },
        error: error => this.error.set(this.errorMessage(error)),
      });
  }

  private errorMessage(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
      const payload = error.error as { message?: string } | null;
      return payload?.message ?? 'Password could not be changed.';
    }
    return 'Password could not be changed.';
  }
}
