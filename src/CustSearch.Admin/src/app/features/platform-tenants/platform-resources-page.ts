import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Observable, finalize } from 'rxjs';
import { AdminShell } from '../../shared/admin-shell/admin-shell';
import { PlatformTenantApiService } from './platform-tenant-api.service';
import { PageResult, PlatformStoreListItem, PlatformTenantUserListItem } from './platform-tenant.models';

@Component({
  selector:'app-platform-resources-page',
  imports:[AdminShell,FormsModule,RouterLink,DatePipe],
  changeDetection:ChangeDetectionStrategy.OnPush,
  template:`
    <app-admin-shell adminType="platform" [pageTitle]="mode==='users'?'Tenant Users':'Stores'" eyebrow="Platform / Tenant resources">
      <section class="panel toolbar">
        <label><span>Search {{mode==='users'?'users or tenants':'stores or tenants'}}</span><input [(ngModel)]="search" (keyup.enter)="applySearch()"></label>
        <button type="button" (click)="applySearch()">Search</button>
        <span>{{total()}} total</span>
      </section>
      @if(error()){<section class="panel error" role="alert">{{error()}} <button type="button" (click)="load()">Retry</button></section>}
      @if(loading()){<section class="panel" aria-live="polite">Loading...</section>}
      @else if(mode==='users'){
        <section class="panel table-scroll"><table><thead><tr><th>User</th><th>Tenant</th><th>Roles</th><th>Stores</th><th>Status</th><th>Last login</th></tr></thead><tbody>
          @for(user of users();track user.id){<tr><td><b>{{user.displayName}}</b><small>{{user.userName}} · {{user.email}}</small></td><td><a [routerLink]="['/admin/tenants',user.tenantId]">{{user.tenantName}}</a><small>{{user.tenantCode}}</small></td><td>{{user.roles.join(', ')||'No role'}}</td><td>{{user.storeCount}}</td><td><span class="badge" [class.active]="user.isActive">{{user.isActive?'Active':'Inactive'}}</span></td><td>{{user.lastLoginUtc?(user.lastLoginUtc|date:'medium'):'Never'}}</td></tr>}
          @empty{<tr><td colspan="6">No tenant users found.</td></tr>}
        </tbody></table></section>
      }@else{
        <section class="panel table-scroll"><table><thead><tr><th>Store</th><th>Tenant</th><th>Location</th><th>Users</th><th>Cameras</th><th>Status</th><th>Updated</th></tr></thead><tbody>
          @for(store of stores();track store.id){<tr><td><b>{{store.storeName}}</b><small>{{store.storeCode}}</small></td><td><a [routerLink]="['/admin/tenants',store.tenantId]">{{store.tenantName}}</a><small>{{store.tenantCode}}</small></td><td>{{store.city}}, {{store.stateOrProvince}}</td><td>{{store.userCount}}</td><td>{{store.cameraCount}}</td><td><span class="badge" [class.active]="store.isActive">{{store.isActive?'Active':'Inactive'}}</span></td><td>{{store.updatedUtc|date:'mediumDate'}}</td></tr>}
          @empty{<tr><td colspan="7">No stores found.</td></tr>}
        </tbody></table></section>
      }
      <nav class="pager" aria-label="Pagination"><button type="button" (click)="previous()" [disabled]="page()===1">Previous</button><span>Page {{page()}}</span><button type="button" (click)="next()" [disabled]="page()*pageSize>=total()">Next</button></nav>
    </app-admin-shell>`,
  styles:[`:host{display:block}.panel{background:var(--color-surface);border:1px solid var(--color-border);border-radius:var(--radius-sm);margin-bottom:1rem;padding:1rem}.toolbar{align-items:end;display:flex;gap:.75rem}.toolbar label{display:grid;flex:1;gap:.35rem}.toolbar input{background:var(--color-background);border:1px solid var(--color-border);color:var(--color-text);padding:.65rem}.toolbar button,.pager button,.error button{background:var(--color-accent);border:0;border-radius:var(--radius-sm);color:var(--color-on-accent);font-weight:700;padding:.65rem .9rem}.table-scroll{overflow:auto}table{border-collapse:collapse;width:100%}th,td{border-bottom:1px solid var(--color-border);padding:.7rem;text-align:left;vertical-align:top}td small{color:var(--color-muted);display:block;margin-top:.2rem}.badge{border-radius:999px;background:var(--color-border);padding:.2rem .5rem}.badge.active{background:color-mix(in srgb,var(--color-success) 18%,transparent);color:var(--color-success)}.pager{align-items:center;display:flex;justify-content:flex-end;gap:.75rem}.pager button:disabled{opacity:.5}.error{color:var(--color-danger)}@media(max-width:700px){.toolbar{align-items:stretch;flex-direction:column}}`],
})
export class PlatformResourcesPage implements OnInit{
  private readonly api=inject(PlatformTenantApiService);private readonly route=inject(ActivatedRoute);
  protected readonly mode=this.route.snapshot.data['mode'] as 'users'|'stores';
  protected readonly users=signal<PlatformTenantUserListItem[]>([]);protected readonly stores=signal<PlatformStoreListItem[]>([]);
  protected readonly loading=signal(true);protected readonly error=signal('');protected readonly page=signal(1);protected readonly total=signal(0);protected readonly pageSize=25;protected search='';
  ngOnInit():void{this.load();}
  protected load():void{this.loading.set(true);this.error.set('');const request=(this.mode==='users'?this.api.tenantUsers(this.page(),this.pageSize,this.search.trim()):this.api.stores(this.page(),this.pageSize,this.search.trim())) as Observable<PageResult<PlatformTenantUserListItem|PlatformStoreListItem>>;request.pipe(finalize(()=>this.loading.set(false))).subscribe({next:value=>{this.total.set(value.totalCount);if(this.mode==='users')this.users.set(value.items as PlatformTenantUserListItem[]);else this.stores.set(value.items as PlatformStoreListItem[]);},error:()=>this.error.set(`Unable to load ${this.mode==='users'?'tenant users':'stores'}.`)});}
  protected applySearch():void{this.page.set(1);this.load();}protected previous():void{if(this.page()>1){this.page.update(value=>value-1);this.load();}}protected next():void{if(this.page()*this.pageSize<this.total()){this.page.update(value=>value+1);this.load();}}
}
