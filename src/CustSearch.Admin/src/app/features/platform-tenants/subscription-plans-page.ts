import { CurrencyPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { AuthSessionService } from '../../core/auth/auth-session.service';
import { PERMISSIONS } from '../../core/auth/permission-catalog';
import { AdminShell } from '../../shared/admin-shell/admin-shell';
import { PlatformTenantApiService } from './platform-tenant-api.service';
import { SaveSubscriptionPlanRequest, SubscriptionPlanOption } from './platform-tenant.models';

/** Manages reusable subscription plans with optimistic versions and server-enforced permissions. */
@Component({selector:'app-subscription-plans-page',imports:[AdminShell,ReactiveFormsModule,CurrencyPipe],templateUrl:'./subscription-plans-page.html',styleUrl:'./tenant-management.scss',changeDetection:ChangeDetectionStrategy.OnPush})
export class SubscriptionPlansPage implements OnInit {
  private readonly api=inject(PlatformTenantApiService);private readonly fb=inject(FormBuilder);protected readonly session=inject(AuthSessionService);protected readonly permissions=PERMISSIONS;protected readonly plans=signal<SubscriptionPlanOption[]>([]);protected readonly busy=signal(false);protected readonly error=signal('');protected readonly editing=signal<SubscriptionPlanOption|null>(null);
  protected readonly form=this.fb.nonNullable.group({planCode:['',Validators.required],planName:['',Validators.required],monthlyPrice:[0,Validators.min(0)],annualPrice:[''],maxStores:[1,Validators.min(1)],maxUsers:[1,Validators.min(1)],maxCameras:[1,Validators.min(1)],maxMonthlyRecognitions:[''],maxMonthlyApiCalls:[''],isActive:[true]});
  ngOnInit():void{this.load();}protected load():void{this.api.plans().subscribe({next:v=>this.plans.set(v),error:()=>this.error.set('Subscription plans could not be loaded.')});}
  protected edit(p:SubscriptionPlanOption):void{this.editing.set(p);this.form.patchValue({...p,annualPrice:p.annualPrice===null?'':String(p.annualPrice),maxMonthlyRecognitions:p.maxMonthlyRecognitions===null?'':String(p.maxMonthlyRecognitions),maxMonthlyApiCalls:p.maxMonthlyApiCalls===null?'':String(p.maxMonthlyApiCalls)});}
  protected reset():void{this.editing.set(null);this.form.reset({planCode:'',planName:'',monthlyPrice:0,annualPrice:'',maxStores:1,maxUsers:1,maxCameras:1,maxMonthlyRecognitions:'',maxMonthlyApiCalls:'',isActive:true});}
  protected save():void{if(this.form.invalid)return;const v=this.form.getRawValue(),current=this.editing(),nullable=(x:string)=>x?Number(x):null;const request:SaveSubscriptionPlanRequest={planCode:v.planCode,planName:v.planName,monthlyPrice:v.monthlyPrice,annualPrice:nullable(v.annualPrice),maxStores:v.maxStores,maxUsers:v.maxUsers,maxCameras:v.maxCameras,maxMonthlyRecognitions:nullable(v.maxMonthlyRecognitions),maxMonthlyApiCalls:nullable(v.maxMonthlyApiCalls),isActive:v.isActive,expectedVersion:current?.version??null};this.busy.set(true);(current?this.api.updatePlan(current.id,request):this.api.createPlan(request)).pipe(finalize(()=>this.busy.set(false))).subscribe({next:()=>{this.reset();this.load();},error:e=>this.error.set(e.status===409?'This plan changed elsewhere. Reload before retrying.':'Plan could not be saved.')});}
}
