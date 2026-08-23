import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AdminShell } from '../../shared/admin-shell/admin-shell';
import { PhaseFiveApiService, Staff } from './phase-five-api.service';

/** Phase 5D staff detail route. It exposes profile/store assignment only; scheduling remains operational API data. */
@Component({
  selector: 'app-phase-five-staff-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, AdminShell],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styles: [`:host{display:block}.wrap{padding:24px}.card{max-width:760px;border:1px solid #e2e2e2;border-radius:12px;padding:20px;background:var(--mat-sys-surface,#fff)}dl{display:grid;grid-template-columns:180px 1fr;gap:12px}dt{font-weight:600}.muted{opacity:.65}.error{color:#b3261e}`],
  template: `<app-admin-shell><main class="wrap"><p><a routerLink="/customer-admin/staff">← Staff</a></p>@if(error()){<p class="error">{{error()}}</p>}@if(staff()){<section class="card"><h1>{{staff()!.firstName}} {{staff()!.lastName}}</h1><p class="muted">Phase 5 staff profile and store assignment</p><dl><dt>Employee code</dt><dd>{{staff()!.employeeCode}}</dd><dt>Mobile</dt><dd>{{staff()!.mobile||'-'}}</dd><dt>Assigned stores</dt><dd>{{staff()!.storeIds.join(', ')||'-'}}</dd><dt>Status</dt><dd>{{staff()!.isActive?'Active':'Inactive'}}</dd></dl><p class="muted">Shift/presence events are operational signals only and are not authoritative payroll or employment-decision evidence.</p></section>}</main></app-admin-shell>`
})
export class PhaseFiveStaffDetailPage implements OnInit {
  private readonly api=inject(PhaseFiveApiService); private readonly route=inject(ActivatedRoute);
  protected readonly staff=signal<Staff|null>(null); protected readonly error=signal('');
  ngOnInit(){const id=Number(this.route.snapshot.paramMap.get('id'));if(!Number.isInteger(id)||id<=0){this.error.set('Invalid staff id.');return;}this.api.staffById(id).subscribe({next:x=>this.staff.set(x),error:e=>this.error.set(this.errorMessage(e))});}
  private errorMessage(error:unknown){if(error instanceof HttpErrorResponse){const payload=error.error as {message?:string}|null;return payload?.message??error.message??'Request failed.';}return error instanceof Error?error.message:'Request failed.';}
}
