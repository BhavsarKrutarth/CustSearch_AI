import { CurrencyPipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AdminShell } from '../../shared/admin-shell/admin-shell';
import { PlatformTenantApiService } from '../platform-tenants/platform-tenant-api.service';
import { PlatformDashboardSummary } from '../platform-tenants/platform-tenant.models';

/** Renders authoritative platform portfolio totals from the Phase 4 dashboard endpoint. */
@Component({selector:'app-platform-dashboard',imports:[AdminShell,RouterLink,CurrencyPipe,DecimalPipe],templateUrl:'./platform-dashboard.html',styleUrl:'./platform-dashboard.scss',changeDetection:ChangeDetectionStrategy.OnPush})
export class PlatformDashboard implements OnInit {
  private readonly api=inject(PlatformTenantApiService); protected readonly data=signal<PlatformDashboardSummary|null>(null); protected readonly loading=signal(true); protected readonly error=signal('');
  ngOnInit():void {this.load();}
  protected load():void {this.loading.set(true);this.error.set('');this.api.dashboard().pipe(finalize(()=>this.loading.set(false))).subscribe({next:v=>this.data.set(v),error:()=>this.error.set('Platform dashboard could not be loaded.')});}
}
