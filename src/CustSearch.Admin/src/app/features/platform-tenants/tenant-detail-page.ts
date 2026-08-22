import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthSessionService } from '../../core/auth/auth-session.service';
import { PERMISSIONS } from '../../core/auth/permission-catalog';
import { AdminShell } from '../../shared/admin-shell/admin-shell';
import { PlatformTenantApiService } from './platform-tenant-api.service';
import { AssignTenantSubscriptionRequest, PlatformTenantDetail, PlatformTenantSummary, SubscriptionPlanOption, TenantAuditItem, TenantUsageItem } from './platform-tenant.models';

/** Presents one tenant control center and exposes only actions/data granted to the active platform operator. */
@Component({selector:'app-tenant-detail-page',imports:[AdminShell,RouterLink,FormsModule,ReactiveFormsModule,DatePipe,DecimalPipe],templateUrl:'./tenant-detail-page.html',styleUrl:'./tenant-management.scss',changeDetection:ChangeDetectionStrategy.OnPush})
export class TenantDetailPage implements OnInit {
  private readonly api=inject(PlatformTenantApiService); private readonly route=inject(ActivatedRoute); private readonly fb=inject(FormBuilder);
  protected readonly session=inject(AuthSessionService); protected readonly permissions=PERMISSIONS; protected readonly tenant=signal<PlatformTenantDetail|null>(null); protected readonly summary=signal<PlatformTenantSummary|null>(null); protected readonly usage=signal<TenantUsageItem[]>([]); protected readonly audit=signal<TenantAuditItem[]>([]); protected readonly plans=signal<SubscriptionPlanOption[]>([]); protected readonly loading=signal(true); protected readonly actionBusy=signal(false); protected readonly error=signal(''); protected readonly notice=signal(''); protected readonly tenantId=Number(this.route.snapshot.paramMap.get('tenantId'));
  protected suspendReason='';
  protected readonly subscriptionForm=this.fb.nonNullable.group({subscriptionPlanId:['',Validators.required],billingCycle:['Monthly',Validators.required],status:['Active',Validators.required],startsUtc:[new Date().toISOString().slice(0,10),Validators.required],endsUtc:[''],autoRenew:[true],maxStores:[''],maxUsers:[''],maxCameras:[''],maxMonthlyRecognitions:[''],maxMonthlyApiCalls:[''],auditReason:['',[Validators.required,Validators.minLength(3),Validators.maxLength(500)]]});
  ngOnInit():void { this.load(); }
  protected load():void { this.loading.set(true);this.error.set('');this.api.get(this.tenantId).pipe(finalize(()=>this.loading.set(false))).subscribe({next:t=>{this.tenant.set(t);this.subscriptionForm.patchValue({subscriptionPlanId:t.plan?.id ? String(t.plan.id):'',status:t.subscriptionStatus,maxStores:String(t.maxStores),maxUsers:String(t.maxUsers),maxCameras:String(t.maxCameras),startsUtc:(t.subscriptionStartsUtc??new Date().toISOString()).slice(0,10),endsUtc:t.subscriptionEndsUtc?.slice(0,10)??''});this.loadPermissionData();},error:()=>this.error.set('Tenant could not be loaded. Verify access and try again.')}); }
  /** Calls optional endpoints only when the authenticated session proves the matching permission. */
  private loadPermissionData():void {
    if(this.session.hasPermission(this.permissions.tenantsOperationalSummary))this.api.summary(this.tenantId).subscribe({next:v=>this.summary.set(v)});
    if(this.session.hasPermission(this.permissions.tenantsViewUsage))this.api.usage(this.tenantId).subscribe({next:v=>this.usage.set(v)});
    if(this.session.hasPermission(this.permissions.platformAuditView))this.api.audit(this.tenantId).subscribe({next:v=>this.audit.set(v.items)});
    if(this.session.hasPermission(this.permissions.subscriptionPlansView)||this.session.hasPermission(this.permissions.subscriptionPlansManage))this.api.plans().subscribe({next:v=>this.plans.set(v.filter(p=>p.isActive))});
  }
  protected activate():void {const t=this.tenant();if(!t)return;this.perform(this.api.activate(t.id,t.version),'Tenant activated.');}
  protected suspend():void {const t=this.tenant();if(!t||this.suspendReason.trim().length<3){this.error.set('Enter a suspension reason of at least 3 characters.');return;}this.perform(this.api.suspend(t.id,this.suspendReason.trim(),t.version),'Tenant suspended.');}
  protected assignSubscription():void {const t=this.tenant();if(!t||this.subscriptionForm.invalid){this.subscriptionForm.markAllAsTouched();return;}const v=this.subscriptionForm.getRawValue();const numberOrNull=(x:string)=>x?Number(x):null;const request:AssignTenantSubscriptionRequest={subscriptionPlanId:Number(v.subscriptionPlanId),billingCycle:v.billingCycle,status:v.status,startsUtc:new Date(v.startsUtc).toISOString(),endsUtc:v.endsUtc?new Date(v.endsUtc).toISOString():null,autoRenew:v.autoRenew,maxStores:numberOrNull(v.maxStores),maxUsers:numberOrNull(v.maxUsers),maxCameras:numberOrNull(v.maxCameras),maxMonthlyRecognitions:numberOrNull(v.maxMonthlyRecognitions),maxMonthlyApiCalls:numberOrNull(v.maxMonthlyApiCalls),expectedVersion:t.version,auditReason:v.auditReason.trim()};this.perform(this.api.assignSubscription(t.id,request),'Subscription and quotas updated.');}
  private perform(request$:ReturnType<PlatformTenantApiService['activate']>,success:string):void {this.actionBusy.set(true);this.error.set('');request$.pipe(finalize(()=>this.actionBusy.set(false))).subscribe({next:t=>{this.tenant.set(t);this.notice.set(success);this.loadPermissionData();},error:e=>this.error.set(e.status===409?'This tenant changed elsewhere. Reload before retrying.':'The requested change could not be completed.')});}
  protected percent(value:number,maximum:number):number{return maximum>0?Math.min(100,Math.round(value/maximum*100)):0;}
}
