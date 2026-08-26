import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AdminShell } from '../../shared/admin-shell/admin-shell';
import { PlatformTenantApiService } from './platform-tenant-api.service';
import { CreateTenantRequest, PlatformTenantAdministrator, SubscriptionPlanOption, UpdateTenantRequest } from './platform-tenant.models';

/** Provides validated create/edit tenant forms and secure Tenant Admin onboarding/password reset. */
@Component({selector:'app-tenant-editor-page',imports:[AdminShell,ReactiveFormsModule,RouterLink],templateUrl:'./tenant-editor-page.html',styleUrl:'./tenant-management.scss',changeDetection:ChangeDetectionStrategy.OnPush})
export class TenantEditorPage implements OnInit {
  private readonly fb=inject(FormBuilder); private readonly api=inject(PlatformTenantApiService); private readonly route=inject(ActivatedRoute); private readonly router=inject(Router);
  protected readonly plans=signal<SubscriptionPlanOption[]>([]); protected readonly busy=signal(false); protected readonly passwordBusy=signal(false); protected readonly error=signal(''); protected readonly message=signal(''); protected readonly showPassword=signal(false); protected readonly administrator=signal<PlatformTenantAdministrator|null>(null); protected readonly tenantId=Number(this.route.snapshot.paramMap.get('tenantId'))||null; private version='';
  private readonly strongPassword=/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{10,500}$/;
  protected readonly form=this.fb.nonNullable.group({legalName:['',[Validators.required,Validators.maxLength(200)]],displayName:['',[Validators.required,Validators.maxLength(150)]],primaryContactName:['',[Validators.required,Validators.maxLength(150)]],primaryEmail:['',[Validators.required,Validators.email]],primaryMobile:[''],countryCode:['IN',[Validators.required,Validators.minLength(2),Validators.maxLength(2)]],timeZone:['Asia/Kolkata',Validators.required],currencyCode:['INR',[Validators.required,Validators.minLength(3),Validators.maxLength(3)]],planId:[''],maxStores:[''],maxUsers:[''],maxCameras:[''],auditReason:['',[Validators.maxLength(500)]],adminUserName:['',[Validators.required,Validators.maxLength(100)]],adminPassword:['',[Validators.required,Validators.pattern(this.strongPassword)]],confirmAdminPassword:['',[Validators.required,Validators.pattern(this.strongPassword)]]});
  protected readonly passwordForm=this.fb.nonNullable.group({newPassword:['',[Validators.required,Validators.pattern(this.strongPassword)]],confirmNewPassword:['',[Validators.required,Validators.pattern(this.strongPassword)]]});

  ngOnInit():void {
    this.api.plans().subscribe({next:value=>this.plans.set(value),error:()=>this.plans.set([])});
    if(!this.tenantId)return;
    this.form.controls.planId.disable();this.form.controls.maxStores.disable();this.form.controls.maxUsers.disable();this.form.controls.maxCameras.disable();this.form.controls.adminUserName.disable();this.form.controls.adminPassword.disable();this.form.controls.confirmAdminPassword.disable();
    this.busy.set(true);
    this.api.get(this.tenantId).pipe(finalize(()=>this.busy.set(false))).subscribe({next:tenant=>{this.version=tenant.version;this.form.patchValue({legalName:tenant.legalName,displayName:tenant.displayName,primaryContactName:tenant.primaryContactName,primaryEmail:tenant.primaryEmail,primaryMobile:tenant.primaryMobile??'',countryCode:tenant.countryCode,timeZone:tenant.timeZone,currencyCode:tenant.currencyCode});},error:error=>this.error.set(this.apiMessage(error,'Tenant profile could not be loaded.'))});
    this.api.administrator(this.tenantId).subscribe({next:value=>this.administrator.set(value),error:error=>this.error.set(this.apiMessage(error,'Tenant administrator could not be loaded.'))});
  }

  protected save():void {
    if(this.form.invalid){this.form.markAllAsTouched();return;}
    const value=this.form.getRawValue();
    if(!this.tenantId&&value.adminPassword!==value.confirmAdminPassword){this.error.set('Administrator password and confirmation do not match.');return;}
    const hasQuotaOverride=Boolean(value.maxStores||value.maxUsers||value.maxCameras);
    if(!this.tenantId&&hasQuotaOverride&&value.auditReason.trim().length<3){this.form.controls.auditReason.setErrors({auditReasonRequired:true});this.error.set('Explain custom quota overrides with an audit reason of at least 3 characters.');return;}
    this.busy.set(true);this.error.set('');this.message.set('');
    const request$=this.tenantId
      ?this.api.update(this.tenantId,{legalName:value.legalName,displayName:value.displayName,timeZone:value.timeZone,primaryContactName:value.primaryContactName,primaryEmail:value.primaryEmail,primaryMobile:value.primaryMobile||null,countryCode:value.countryCode.toUpperCase(),currencyCode:value.currencyCode.toUpperCase(),expectedVersion:this.version} satisfies UpdateTenantRequest)
      :this.api.create({legalName:value.legalName,displayName:value.displayName,timeZone:value.timeZone,primaryContactName:value.primaryContactName,primaryEmail:value.primaryEmail,primaryMobile:value.primaryMobile||null,countryCode:value.countryCode.toUpperCase(),currencyCode:value.currencyCode.toUpperCase(),planId:value.planId?Number(value.planId):null,maxStores:value.maxStores?Number(value.maxStores):null,maxUsers:value.maxUsers?Number(value.maxUsers):null,maxCameras:value.maxCameras?Number(value.maxCameras):null,auditReason:value.auditReason.trim()||null,adminUserName:value.adminUserName,adminPassword:value.adminPassword,confirmAdminPassword:value.confirmAdminPassword} satisfies CreateTenantRequest);
    request$.pipe(finalize(()=>this.busy.set(false))).subscribe({next:tenant=>void this.router.navigate(['/admin/tenants',tenant.id]),error:error=>this.error.set(error.status===409?'This tenant changed elsewhere. Reload before saving.':this.apiMessage(error,'Tenant could not be saved. Check the form and retry.'))});
  }

  protected resetPassword():void {
    if(!this.tenantId||this.passwordForm.invalid){this.passwordForm.markAllAsTouched();return;}
    const value=this.passwordForm.getRawValue();
    if(value.newPassword!==value.confirmNewPassword){this.error.set('New password and confirmation do not match.');return;}
    this.passwordBusy.set(true);this.error.set('');this.message.set('');
    this.api.resetAdministratorPassword(this.tenantId,value).pipe(finalize(()=>this.passwordBusy.set(false))).subscribe({next:administrator=>{this.administrator.set(administrator);this.passwordForm.reset();this.showPassword.set(false);this.message.set(`Password reset for ${administrator.userName}. Existing sessions were revoked.`);},error:error=>this.error.set(this.apiMessage(error,'Password could not be reset.'))});
  }

  protected togglePassword():void{this.showPassword.update(value=>!value);}
  private apiMessage(error:unknown,fallback:string):string{const candidate=error as {error?:{message?:unknown;detail?:unknown}};return typeof candidate.error?.message==='string'?candidate.error.message:typeof candidate.error?.detail==='string'?candidate.error.detail:fallback;}
}
