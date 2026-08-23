import { ChangeDetectionStrategy, Component, computed, inject, input, OnInit } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthSessionService } from '../../core/auth/auth-session.service';
import { ADMIN_NAVIGATION } from '../../core/navigation/admin-navigation';
import { ThemePreference, ThemeService } from '../../core/theme/theme.service';

@Component({
  selector: 'app-admin-shell',
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './admin-shell.html',
  styleUrl: './admin-shell.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
/** Renders a responsive admin shell whose navigation is filtered by server-issued permissions. */
export class AdminShell implements OnInit {
  protected readonly theme = inject(ThemeService);
  protected readonly session = inject(AuthSessionService);

  // Customer Admin is the safe default for tenant-scoped Phase 5 pages. Platform pages continue
  // to provide their explicit values, so their dark-mode/navigation context remains unchanged.
  readonly adminType = input<'customer' | 'platform'>('customer');
  readonly pageTitle = input('Customer Admin');
  readonly eyebrow = input('Tenant Operations');

  protected readonly themes: ThemePreference[] = ['light', 'dark', 'system'];
  protected readonly navigation = computed(() => ADMIN_NAVIGATION[this.adminType()]
    .filter(item => this.session.hasPermission(item.permission)));

  ngOnInit(): void {
    this.theme.applyContextDefault(this.adminType() === 'platform' ? 'dark' : 'light');
  }

  protected setTheme(value: string): void {
    this.theme.setPreference(value as ThemePreference);
  }
}
