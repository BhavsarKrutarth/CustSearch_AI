import { ChangeDetectionStrategy, Component, computed, effect, inject, input, OnInit, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { catchError, finalize, of } from 'rxjs';
import { AuthApiService } from '../../core/auth/auth-api.service';
import { AuthSessionService } from '../../core/auth/auth-session.service';
import { ADMIN_NAVIGATION, AdminNavigationGroup } from '../../core/navigation/admin-navigation';
import { ThemePreference, ThemeService } from '../../core/theme/theme.service';
import { CsIcon } from '../cs-icon/cs-icon';

@Component({
  selector: 'app-admin-shell',
  imports: [RouterLink, RouterLinkActive, CsIcon],
  templateUrl: './admin-shell.html',
  styleUrl: './admin-shell.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
/** Renders a responsive admin shell whose navigation is filtered by server-issued permissions. */
export class AdminShell implements OnInit {
  protected readonly theme = inject(ThemeService);
  protected readonly session = inject(AuthSessionService);
  private readonly authApi = inject(AuthApiService);
  private readonly router = inject(Router);
  protected readonly logoutBusy = signal(false);
  protected readonly mobileNavOpen = signal(false);
  protected readonly sidebarCollapsed = signal(false);
  protected readonly userMenuOpen = signal(false);

  // Customer Admin is the safe default for tenant-scoped Phase 5 pages. Platform pages continue
  // to provide their explicit values, so their dark-mode/navigation context remains unchanged.
  readonly adminType = input<'customer' | 'platform'>('customer');
  readonly pageTitle = input('Customer Admin');
  readonly eyebrow = input('Tenant Operations');

  protected readonly themes: ThemePreference[] = ['light', 'dark', 'system'];
  protected readonly navigation = computed(() => ADMIN_NAVIGATION[this.adminType()]
    .filter(item => this.session.hasPermission(item.permission)));
  protected readonly navigationGroups = computed<AdminNavigationGroup[]>(() => {
    const groups = new Map<string, AdminNavigationGroup>();
    for (const item of this.navigation()) {
      const label = item.label === 'Dashboard' ? 'Overview'
        : ['Customers', 'Households', 'Visits', 'Visit Party / Co-Visit', 'Products', 'Categories'].includes(item.label) ? 'Customer operations'
          : ['Cameras', 'Camera operations', 'Live monitoring', 'Evidence storage', 'Integrations'].includes(item.label) ? 'Operations'
            : ['Alerts', 'Retail security', 'Recognition review', 'Reports & exports', 'Retail reports', 'Reports'].includes(item.label) ? 'Risk & insights'
              : ['Stores', 'Staff', 'Users', 'Billing', 'Retail invoices', 'Platform Billing', 'Subscriptions'].includes(item.label) ? 'Administration'
                : 'Administration';
      const current = groups.get(label) ?? { label, items: [] };
      groups.set(label, { label, items: [...current.items, item] });
    }
    return [...groups.values()];
  });

  constructor() {
    effect(() => this.theme.setTenantContext(this.adminType() === 'customer' ? this.session.currentUser()?.tenantCode ?? null : null));
  }

  ngOnInit(): void {
    this.theme.applyContextDefault('dark');
  }

  protected setTheme(value: string): void {
    this.theme.setPreference(value as ThemePreference);
  }

  protected toggleMobileNav(): void { this.mobileNavOpen.update(value => !value); }
  protected closeMobileNav(): void { this.mobileNavOpen.set(false); }
  protected toggleSidebar(): void { this.sidebarCollapsed.update(value => !value); }
  protected toggleUserMenu(): void { this.userMenuOpen.update(value => !value); }
  protected closeUserMenu(): void { this.userMenuOpen.set(false); }

  protected initials(): string {
    const name = this.session.currentUser()?.displayName?.trim() || (this.adminType() === 'platform' ? 'Platform Admin' : 'Customer Admin');
    return name.split(/\s+/).slice(0, 2).map(part => part[0]).join('').toUpperCase();
  }

  protected roleLabel(): string {
    return this.adminType() === 'platform'
      ? (this.session.currentUser()?.roles?.[0] ?? 'Platform administrator')
      : (this.session.currentUser()?.roles?.[0] ?? 'Tenant administrator');
  }

  protected canCustomizeTheme(): boolean {
    return this.adminType() === 'customer' && this.session.roles().some(role => ['TenantAdmin', 'TenantOwner', 'ShopOwner'].includes(role));
  }

  protected logout(): void {
    if (this.logoutBusy()) return;
    this.logoutBusy.set(true);
    this.authApi.logout().pipe(
      catchError(() => of(void 0)),
      finalize(() => {
        this.session.clear();
        this.logoutBusy.set(false);
        void this.router.navigateByUrl('/login');
      }),
    ).subscribe();
  }
}
