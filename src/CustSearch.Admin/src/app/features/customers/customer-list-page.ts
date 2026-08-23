import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthSessionService } from '../../core/auth/auth-session.service';
import { PERMISSIONS } from '../../core/auth/permission-catalog';
import { AdminShell } from '../../shared/admin-shell/admin-shell';
import { CustomerApiService, CustomerListItem } from './customer-api.service';

/** Phase 6E customer search/create page. TenantId is never exposed in forms; store filters remain server-authorized. */
@Component({
  selector:'app-customer-list-page', standalone:true, imports:[CommonModule,ReactiveFormsModule,RouterLink,AdminShell],
  changeDetection:ChangeDetectionStrategy.OnPush,
  styles:[`:host{display:block}.wrap{padding:24px}.toolbar{display:flex;gap:12px;justify-content:space-between;align-items:end;flex-wrap:wrap}.filters{display:flex;gap:8px;flex-wrap:wrap}.grid{display:grid;grid-template-columns:minmax(0,1fr) 340px;gap:18px;margin-top:18px}.card{border:1px solid #ddd;border-radius:12px;padding:16px;background:var(--mat-sys-surface,#fff)}table{width:100%;border-collapse:collapse}th,td{text-align:left;padding:10px;border-bottom:1px solid #eee}input,button{min-height:40px;padding:8px;border:1px solid #ccc;border-radius:8px}form{display:grid;gap:9px}.pager{display:flex;gap:8px;align-items:center;margin-top:12px}.muted{opacity:.65}.error{color:#b3261e}.ok{color:#147d32}@media(max-width:900px){.grid{grid-template-columns:1fr}}`],
  template:`<app-admin-shell><main class="wrap"><div class="toolbar"><div><h1>Customers</h1><p class="muted">Phase 6 tenant-scoped shopper CRM</p></div><form class="filters" [formGroup]="searchForm" (ngSubmit)="search()"><input formControlName="search" placeholder="Search code, name, mobile or email"><input type="number" formControlName="storeId" placeholder="Store ID"><label><input type="checkbox" formControlName="activeOnly"> Active only</label><button>Search</button></form></div>
  @if(error()){<p class="error">{{error()}}</p>} @if(message()){<p class="ok">{{message()}}</p>}
  <div class="grid"><section class="card"><table><thead><tr><th>Code</th><th>Customer</th><th>Contact</th><th>Stores</th><th>Status</th></tr></thead><tbody>@for(x of customers();track x.id){<tr><td><a [routerLink]="['/customer-admin/customers',x.id]">{{x.customerCode}}</a></td><td>{{x.firstName}} {{x.lastName||''}}</td><td>{{x.mobile||x.email||'-'}}</td><td>{{x.storeIds.join(', ')||'Tenant-wide'}}</td><td>{{x.isActive?'Active':'Inactive'}}</td></tr>}@empty{<tr><td colspan="5">No customers found.</td></tr>}</tbody></table><div class="pager"><button type="button" [disabled]="pageNumber()<=1" (click)="previous()">Previous</button><span>Page {{pageNumber()}} · {{totalCount()}} records</span><button type="button" [disabled]="pageNumber()*pageSize()>=totalCount()" (click)="next()">Next</button></div></section>
  @if(canCreate()){<aside class="card"><h2>Add customer</h2><form [formGroup]="createForm" (ngSubmit)="create()"><input formControlName="customerCode" placeholder="Customer code (optional)"><input formControlName="firstName" placeholder="First name"><input formControlName="lastName" placeholder="Last name"><input formControlName="mobile" placeholder="Mobile"><input formControlName="email" placeholder="Email"><input formControlName="notes" placeholder="Notes"><input formControlName="storeIds" placeholder="Store IDs comma separated"><input type="number" formControlName="primaryStoreId" placeholder="Primary store ID"><button [disabled]="createForm.invalid||saving()">Create customer</button></form></aside>}</div></main></app-admin-shell>`
})
export class CustomerListPage implements OnInit {
  private readonly api=inject(CustomerApiService); private readonly session=inject(AuthSessionService); private readonly fb=inject(FormBuilder);
  protected readonly customers=signal<CustomerListItem[]>([]); protected readonly pageNumber=signal(1); protected readonly pageSize=signal(25); protected readonly totalCount=signal(0); protected readonly saving=signal(false); protected readonly error=signal(''); protected readonly message=signal('');
  protected readonly canCreate=computed(()=>this.session.hasPermission(PERMISSIONS.customersCreate));
  protected readonly searchForm=this.fb.nonNullable.group({search:'',storeId:'',activeOnly:false});
  protected readonly createForm=this.fb.nonNullable.group({customerCode:'',firstName:['',Validators.required],lastName:'',mobile:'',email:['',Validators.email],notes:'',storeIds:'',primaryStoreId:''});
  ngOnInit(){this.load();}
  protected search(){this.pageNumber.set(1);this.load();}
  protected previous(){this.pageNumber.update(x=>Math.max(1,x-1));this.load();}
  protected next(){if(this.pageNumber()*this.pageSize()<this.totalCount()){this.pageNumber.update(x=>x+1);this.load();}}
  protected create(){if(this.createForm.invalid)return;this.saving.set(true);this.clearStatus();const v=this.createForm.getRawValue();const stores=this.ids(v.storeIds);this.api.create({customerCode:v.customerCode.trim()||null,firstName:v.firstName,lastName:v.lastName.trim()||null,mobile:v.mobile.trim()||null,email:v.email.trim()||null,notes:v.notes.trim()||null,storeIds:stores,primaryStoreId:this.numberOrNull(v.primaryStoreId)}).subscribe({next:()=>{this.saving.set(false);this.message.set('Customer created.');this.createForm.reset({customerCode:'',firstName:'',lastName:'',mobile:'',email:'',notes:'',storeIds:'',primaryStoreId:''});this.load(false);},error:e=>this.fail(e)});}
  private load(clear=true){if(clear)this.clearStatus();const v=this.searchForm.getRawValue();const filters:Record<string,string|number|boolean>={activeOnly:v.activeOnly};const storeId=this.numberOrNull(v.storeId);if(storeId)filters['storeId']=storeId;this.api.search({pageNumber:this.pageNumber(),pageSize:this.pageSize(),search:v.search.trim()||undefined,filters}).subscribe({next:r=>{this.customers.set(r.data);this.totalCount.set(r.totalCount);},error:e=>this.fail(e)});}
  private ids(v:string){return v.split(',').map(x=>Number(x.trim())).filter(x=>Number.isInteger(x)&&x>0);}
  private numberOrNull(v:string){const n=Number(v);return Number.isInteger(n)&&n>0?n:null;}
  private clearStatus(){this.error.set('');this.message.set('');}
  private fail(e:unknown){this.saving.set(false);this.error.set(e instanceof HttpErrorResponse?(e.error?.message??e.message):'Request failed.');}
}
