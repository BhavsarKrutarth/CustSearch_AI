import {ChangeDetectionStrategy,Component,OnInit,computed,inject,signal} from '@angular/core';
import {FormsModule} from '@angular/forms';
import {ActivatedRoute,RouterLink} from '@angular/router';
import {AuthSessionService} from '../../core/auth/auth-session.service';
import {PERMISSIONS} from '../../core/auth/permission-catalog';
import {AdminShell} from '../../shared/admin-shell/admin-shell';
import {SaveStoragePolicy,StorageApiService,StorageSummary} from './storage-api.service';

@Component({
  selector:'app-platform-storage-page',
  imports:[AdminShell,FormsModule,RouterLink],
  changeDetection:ChangeDetectionStrategy.OnPush,
  template:`
    <app-admin-shell adminType="platform" eyebrow="Platform / Tenant Storage" pageTitle="Evidence storage policy">
      <p><a [routerLink]="['/admin/tenants',tenantId]">Back to tenant control center</a></p>
      @if(error()){<p class="error" role="alert">{{error()}}</p>}
      @if(summary();as s){
        <section class="usage">
          <b>{{gb(s.usage.usedBytes)}} / {{gb(s.usage.quotaBytes)}} GB</b>
          <span>{{s.usage.usagePercent}}% | {{s.usage.pressureLevel}} | {{gb(s.usage.availableBytes)}} GB available</span>
        </section>
        @if(canManage()){
          <form (ngSubmit)="save()">
            <label><input type="checkbox" name="enabled" [(ngModel)]="form.storageEnabled"> Storage enabled</label>
            <label>Tenant quota (GB)<input type="number" name="quota" min="0.001" max="10240" step="0.25" [(ngModel)]="quotaGb" required></label>
            <label>Default retention days<input type="number" name="default" min="1" max="3650" [(ngModel)]="form.defaultRetentionDays" required></label>
            <label>Snapshot retention days<input type="number" name="snapshot" min="1" max="3650" [(ngModel)]="form.motionSnapshotRetentionDays" required></label>
            <label>Motion clip retention days<input type="number" name="clip" min="1" max="3650" [(ngModel)]="form.motionClipRetentionDays" required></label>
            <label>False positive retention days<input type="number" name="falsePositive" min="1" max="3650" [(ngModel)]="form.falsePositiveRetentionDays" required></label>
            <label>Unreviewed evidence days<input type="number" name="unreviewed" min="1" max="3650" [(ngModel)]="form.unreviewedEvidenceRetentionDays" required></label>
            <label>Confirmed incident days<input type="number" name="confirmed" min="1" max="3650" [(ngModel)]="form.confirmedIncidentRetentionDays" required></label>
            <label>Warning %<input type="number" name="warning" min="1" max="99" [(ngModel)]="form.warningPercent" required></label>
            <label>Critical %<input type="number" name="critical" min="2" max="100" [(ngModel)]="form.criticalPercent" required></label>
            <label><input type="checkbox" name="snapshots" [(ngModel)]="form.allowSnapshots"> Allow snapshots</label>
            <label><input type="checkbox" name="clips" [(ngModel)]="form.allowMotionClips"> Allow motion clips</label>
            <label><input type="checkbox" name="cleanup" [(ngModel)]="form.autoCleanupEnabled"> Auto cleanup</label>
            <button type="submit">Save storage policy</button>
            @if(message()){<span role="status">{{message()}}</span>}
          </form>
        }@else{
          <p class="readonly">Storage policy is read-only for your platform role.</p>
        }
      }@else if(!error()){<p>Loading policy...</p>}
    </app-admin-shell>`,
  styles:[`
    .usage,form,.readonly{background:var(--color-surface);border:1px solid var(--color-border);border-radius:var(--radius-sm);padding:1rem}
    .usage{display:grid;margin-bottom:1rem}.usage b{font-size:1.5rem}
    form{display:grid;grid-template-columns:repeat(2,1fr);gap:1rem}label{display:grid;gap:.35rem}
    input{background:var(--color-background);border:1px solid var(--color-border);border-radius:var(--radius-sm);color:var(--color-text);padding:.6rem}
    button{background:var(--color-accent);border:0;border-radius:var(--radius-sm);font-weight:700;padding:.7rem}.error{color:var(--color-danger)}
    @media(max-width:800px){form{grid-template-columns:1fr}}
  `],
})
export class PlatformStoragePage implements OnInit {
  private readonly api=inject(StorageApiService);
  private readonly route=inject(ActivatedRoute);
  private readonly session=inject(AuthSessionService);
  protected readonly canManage=computed(()=>this.session.hasPermission(PERMISSIONS.platformTenantStorageManage));
  protected readonly tenantId=Number(this.route.snapshot.paramMap.get('tenantId'));
  protected readonly summary=signal<StorageSummary|null>(null);
  protected readonly error=signal('');
  protected readonly message=signal('');
  protected quotaGb=2;
  protected form:SaveStoragePolicy={storageEnabled:true,storageQuotaBytes:2147483648,defaultRetentionDays:15,motionSnapshotRetentionDays:15,motionClipRetentionDays:15,falsePositiveRetentionDays:3,unreviewedEvidenceRetentionDays:15,confirmedIncidentRetentionDays:30,warningPercent:80,criticalPercent:90,allowSnapshots:true,allowMotionClips:true,autoCleanupEnabled:true,quotaPressurePolicy:1};

  ngOnInit():void {
    this.api.platform(this.tenantId).subscribe({next:value=>this.set(value),error:()=>this.error.set('Tenant storage policy could not be loaded.')});
  }

  protected save():void {
    if(!this.canManage())return;
    this.form.storageQuotaBytes=Math.round(this.quotaGb*1073741824);
    this.api.savePlatform(this.tenantId,this.form).subscribe({
      next:value=>{this.set(value);this.message.set('Storage policy saved.');},
      error:error=>this.error.set(error.error?.message||error.error?.detail||'Storage policy was rejected.'),
    });
  }

  private set(value:StorageSummary):void {
    this.summary.set(value);
    this.form={...value.policy,quotaPressurePolicy:1};
    this.quotaGb=value.policy.storageQuotaBytes/1073741824;
  }

  protected gb(value:number):string{return(value/1073741824).toFixed(2);}
}
