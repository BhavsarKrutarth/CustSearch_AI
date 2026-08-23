import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthSessionService } from '../../core/auth/auth-session.service';
import { PERMISSIONS } from '../../core/auth/permission-catalog';
import { AdminShell } from '../../shared/admin-shell/admin-shell';
import { HouseholdApiService, HouseholdListItem } from './household-api.service';

/** Phase 7E household search/create page. A household is verified family data, not a CCTV co-visit party. */
@Component({
 selector:'app-household-list-page',standalone:true,imports:[CommonModule,ReactiveFormsModule,RouterLink,AdminShell],changeDetection:ChangeDetectionStrategy.OnPush,
 styles:[`:host{display:block}.wrap{padding:24px}.toolbar{display:flex;gap:12px;justify-content:space-between;align-items:end;flex-wrap:wrap}.filters{display:flex;gap:8px;flex-wrap:wrap}.grid{display:grid;grid-template-columns:minmax(0,1fr) 340px;gap:18px;margin-top:18px}.card{border:1px solid #ddd;border-radius:12px;padding:16px;background:var(--mat-sys-surface,#fff)}table{width:100%;border-collapse:collapse}th,td{text-align:left;padding:10px;border-bottom:1px solid #eee}input,button{min-height:40px;padding:8px;border:1px solid #ccc;border-radius:8px}form{display:grid;gap:9px}.pager{display:flex;gap:8px;align-items:center;margin-top:12px}.muted{opacity:.65}.error{color:#b3261e}.ok{color:#147d32}@media(max-width:900px){.grid{grid-template-columns:1fr}}`],
 template:`<app-admin-shell><main class="wrap"><div class="toolbar"><div><h1>Households / Families</h1><p class="muted">Verified/customer-provided relationships only. Co-visits do not create family links.</p></div><form class="filters" [formGroup]="searchForm" (ngSubmit)="search()"><input formControlName="search" placeholder="Search household code or name"><label><input type="checkbox" formControlName="activeOnly"> Active only</label><button>Search</button></form></div>
 @if(error()){<p class="error">{{error()}}</p>} @if(message()){<p class="ok">{{message()}}</p>}
 <div class="grid"><section class="card"><table><thead><tr><th>Code</th><th>Household</th><th>Visible members</th><th>Status</th></tr></thead><tbody>@for(x of households();track x.id){<tr><td><a [routerLink]="['/customer-admin/households',x.id]">{{x.householdCode}}</a></td><td>{{x.name}}</td><td>{{x.visibleMemberCount}}</td><td>{{x.isActive?'Active':'Inactive'}}</td></tr>}@empty{<tr><td colspan="4">No households found.</td></tr>}</tbody></table><div class="pager"><button type="button" [disabled]="pageNumber()<=1" (click)="previous()">Previous</button><span>Page {{pageNumber()}} · {{totalCount()}} records</span><button type="button" [disabled]="pageNumber()*pageSize()>=totalCount()" (click)="next()">Next</button></div></section>
 @if(canCreate()){<aside class="card"><h2>Create household</h2><p class="muted">New empty households are created by tenant-wide owner/admin roles; link verified customer members on the detail page.</p><form [formGroup]="createForm" (ngSubmit)="create()"><input formControlName="householdCode" placeholder="Household code (optional)"><input formControlName="name" placeholder="Household name"><input formControlName="notes" placeholder="Notes"><button [disabled]="createForm.invalid||saving()">Create household</button></form></aside>}</div></main></app-admin-shell>`
})
export class HouseholdListPage implements OnInit{
 private readonly api=inject(HouseholdApiService);private readonly session=inject(AuthSessionService);private readonly fb=inject(FormBuilder);
 protected readonly households=signal<HouseholdListItem[]>([]);protected readonly pageNumber=signal(1);protected readonly pageSize=signal(25);protected readonly totalCount=signal(0);protected readonly saving=signal(false);protected readonly error=signal('');protected readonly message=signal('');
 protected readonly canCreate=computed(()=>this.session.hasPermission(PERMISSIONS.householdsCreate));
 protected readonly searchForm=this.fb.nonNullable.group({search:'',activeOnly:false});protected readonly createForm=this.fb.nonNullable.group({householdCode:'',name:['',Validators.required],notes:''});
 ngOnInit(){this.load();} protected search(){this.pageNumber.set(1);this.load();} protected previous(){this.pageNumber.update(x=>Math.max(1,x-1));this.load();} protected next(){if(this.pageNumber()*this.pageSize()<this.totalCount()){this.pageNumber.update(x=>x+1);this.load();}}
 protected create(){if(this.createForm.invalid)return;this.saving.set(true);this.clear();const v=this.createForm.getRawValue();this.api.create({householdCode:v.householdCode.trim()||null,name:v.name.trim(),notes:v.notes.trim()||null}).subscribe({next:()=>{this.saving.set(false);this.message.set('Household created.');this.createForm.reset({householdCode:'',name:'',notes:''});this.load(false);},error:e=>this.fail(e)});}
 private load(clear=true){if(clear)this.clear();const v=this.searchForm.getRawValue();this.api.search({pageNumber:this.pageNumber(),pageSize:this.pageSize(),search:v.search.trim()||undefined,filters:{activeOnly:v.activeOnly}}).subscribe({next:r=>{this.households.set(r.data);this.totalCount.set(r.totalCount);},error:e=>this.fail(e)});}
 private clear(){this.error.set('');this.message.set('');} private fail(e:unknown){this.saving.set(false);this.error.set(e instanceof HttpErrorResponse?(e.error?.message??e.message):'Request failed.');}
}