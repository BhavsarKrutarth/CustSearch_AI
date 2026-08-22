import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthSessionService } from '../../core/auth/auth-session.service';
import { PERMISSIONS } from '../../core/auth/permission-catalog';
import { AdminShell } from '../../shared/admin-shell/admin-shell';
import { PlatformTenantApiService } from './platform-tenant-api.service';
import { PlatformTenantListItem, SubscriptionPlanOption, TenantStatus } from './platform-tenant.models';

/** Presents a searchable, permission-aware platform directory without making tenant state authoritative in the UI. */
@Component({
  selector: 'app-tenant-list-page',
  imports: [AdminShell, FormsModule, RouterLink],
  templateUrl: './tenant-list-page.html',
  styleUrl: './tenant-management.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TenantListPage implements OnInit {
  private readonly api = inject(PlatformTenantApiService);
  protected readonly session = inject(AuthSessionService);
  protected readonly permissions = PERMISSIONS;
  protected readonly tenants = signal<PlatformTenantListItem[]>([]);
  protected readonly plans = signal<SubscriptionPlanOption[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal('');
  protected readonly totalCount = signal(0);
  protected search = '';
  protected status = '';
  protected planId = '';
  protected page = 1;
  protected readonly pageSize = 10;

  ngOnInit(): void {
    this.load();
    this.api.plans().subscribe({ next: plans => this.plans.set(plans), error: () => this.plans.set([]) });
  }

  protected applyFilters(): void {
    this.page = 1;
    this.load();
  }

  protected changePage(direction: -1 | 1): void {
    const target = this.page + direction;
    if (target < 1 || (direction > 0 && this.page * this.pageSize >= this.totalCount())) return;
    this.page = target;
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.error.set('');
    this.api.list({
      page: this.page,
      pageSize: this.pageSize,
      search: this.search.trim() || undefined,
      status: (this.status || undefined) as TenantStatus | undefined,
      planId: this.planId ? Number(this.planId) : undefined,
    }).pipe(finalize(() => this.loading.set(false))).subscribe({
      next: response => {
        this.tenants.set(response.items);
        this.totalCount.set(response.totalCount);
      },
      error: () => this.error.set('Tenant directory could not be loaded. Try again.'),
    });
  }

  protected statusClass(status: TenantStatus): string {
    return status === 'Active' ? 'success' : status === 'Suspended' ? 'warning' : 'danger';
  }
}
