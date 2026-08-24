import { ChangeDetectionStrategy, Component, DestroyRef, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AdminShell } from '../../shared/admin-shell/admin-shell';
import { AuthSessionService } from '../../core/auth/auth-session.service';
import { PERMISSIONS } from '../../core/auth/permission-catalog';
import { AlertStatus, AlertView, AlertsApiService } from './alerts-api.service';
import { AlertsRealtimeService } from './alerts-realtime.service';

@Component({selector:'app-notification-center-page',imports:[AdminShell],changeDetection:ChangeDetectionStrategy.OnPush,template:`
<app-admin-shell pageTitle="Notification center" eyebrow="Alerts & real-time">
  <section class="summary" aria-label="Alert summary"><div><b>{{unreadCount()}}</b><span>Unread</span></div><div><b>{{alerts().length}}</b><span>Loaded alerts</span></div><div><b>{{realtime.connectionState()}}</b><span>Real-time state</span></div><button type="button" (click)="load()">Refresh authoritative state</button></section>
  @if(error()){<p class="error" role="alert">{{error()}}</p>}
  <section class="layout">
    <div class="list" aria-label="Alerts">
      @if(loading()){<p>Loading alerts…</p>}
      @for(alert of alerts();track alert.id){<button type="button" class="alert" [class.selected]="selected()?.id===alert.id" (click)="selected.set(alert)"><span class="severity s{{alert.severity}}"></span><span><b>{{alert.title}}</b><small>{{alert.alertType}} · {{statusName(alert.status)}} · {{alert.createdUtc}}</small><em>{{alert.message}}</em></span></button>}
      @if(!loading()&&alerts().length===0){<p>No alerts in your authorized tenant/store scope.</p>}
    </div>
    <aside class="detail" aria-label="Alert detail">
      @if(selected();as alert){<p class="eyebrow">{{alert.alertType}}</p><h2>{{alert.title}}</h2><p>{{alert.message}}</p><dl><dt>Status</dt><dd>{{statusName(alert.status)}}</dd><dt>Store</dt><dd>{{alert.storeId??'Tenant-wide'}}</dd><dt>Entity</dt><dd>{{alert.entityType}} {{alert.entityId??''}}</dd><dt>Correlation</dt><dd>{{alert.correlationId}}</dd></dl><div class="actions">@if(canAcknowledge()&&alert.status<3){<button type="button" (click)="acknowledge(alert.id)">Acknowledge</button>}@if(canResolve()&&alert.status!==4&&alert.status!==5){<button type="button" class="resolve" (click)="resolve(alert.id)">Resolve</button>}</div>}@else{<p>Select an alert to view its authoritative detail.</p>}
    </aside>
  </section>
</app-admin-shell>`,styles:[`:host{display:block}.summary{display:grid;grid-template-columns:repeat(3,minmax(0,1fr)) auto;gap:.75rem;margin-bottom:1rem}.summary div,.list,.detail{background:var(--color-surface);border:1px solid var(--color-border);border-radius:var(--radius-sm)}.summary div{display:grid;padding:1rem}.summary b{font-size:1.35rem}.summary span,small,.eyebrow,dt{color:var(--color-muted);font-size:.75rem}.summary button,.actions button{background:var(--color-accent);border:0;border-radius:var(--radius-sm);color:var(--color-on-accent);font-weight:700;padding:.75rem 1rem}.layout{display:grid;grid-template-columns:minmax(0,1.4fr) minmax(18rem,.8fr);gap:1rem}.list{display:grid;overflow:hidden}.alert{align-items:flex-start;background:transparent;border:0;border-bottom:1px solid var(--color-border);color:var(--color-text);display:flex;gap:.75rem;padding:1rem;text-align:left}.alert.selected,.alert:hover{background:var(--color-nav-active)}.alert span:nth-child(2){display:grid;gap:.25rem}.alert em{font-style:normal}.severity{border-radius:50%;height:.7rem;margin-top:.35rem;width:.7rem}.s1{background:var(--color-accent)}.s2{background:#d78b16}.s3{background:var(--color-danger)}.detail{padding:1.2rem}.detail h2{margin:.2rem 0 .8rem}.detail dl{display:grid;grid-template-columns:6rem 1fr;gap:.5rem}.detail dd{margin:0;overflow-wrap:anywhere}.actions{display:flex;gap:.6rem;margin-top:1rem}.actions .resolve{background:var(--color-danger)}.error{color:var(--color-danger)}@media(max-width:800px){.summary{grid-template-columns:1fr 1fr}.layout{grid-template-columns:1fr}}`],})
/** Tenant notification center with permission-controlled acknowledgement/resolve and reconnect recovery. */
export class NotificationCenterPage implements OnInit,OnDestroy{
  private readonly api=inject(AlertsApiService);private readonly session=inject(AuthSessionService);private readonly destroyRef=inject(DestroyRef);protected readonly realtime=inject(AlertsRealtimeService);protected readonly alerts=signal<AlertView[]>([]);protected readonly selected=signal<AlertView|null>(null);protected readonly unreadCount=signal(0);protected readonly loading=signal(false);protected readonly error=signal('');protected readonly canAcknowledge=computed(()=>this.session.hasPermission(PERMISSIONS.alertsAcknowledge));protected readonly canResolve=computed(()=>this.session.hasPermission(PERMISSIONS.alertsConfigure));
  ngOnInit():void{this.realtime.events$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(event=>this.upsert(event.alert));void this.realtime.start();this.load();}
  ngOnDestroy():void{void this.realtime.stop();}
  protected load():void{this.loading.set(true);this.error.set('');this.api.list().subscribe({next:value=>{this.alerts.set(value.items);this.unreadCount.set(value.unreadCount);const selected=this.selected();if(selected)this.selected.set(value.items.find(x=>x.id===selected.id)??null);this.loading.set(false);},error:()=>{this.error.set('Unable to load alerts.');this.loading.set(false);}});}
  protected acknowledge(id:number):void{this.api.acknowledge(id).subscribe({next:value=>this.upsert(value),error:()=>this.error.set('Alert acknowledgement was rejected.')});}
  protected resolve(id:number):void{this.api.resolve(id).subscribe({next:value=>this.upsert(value),error:()=>this.error.set('Alert resolution was rejected.')});}
  protected statusName(status:AlertStatus):string{return({1:'New',2:'Delivered',3:'Acknowledged',4:'Resolved',5:'Expired'} as const)[status];}
  private upsert(alert:AlertView):void{this.alerts.update(items=>[alert,...items.filter(x=>x.id!==alert.id)].sort((a,b)=>b.id-a.id));this.selected.update(value=>value?.id===alert.id?alert:value);this.unreadCount.set(this.alerts().filter(x=>x.status===1||x.status===2).length);}
}
