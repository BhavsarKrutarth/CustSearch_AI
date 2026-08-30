import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthSessionService } from '../../core/auth/auth-session.service';
import { PERMISSIONS } from '../../core/auth/permission-catalog';
import { AdminShell } from '../../shared/admin-shell/admin-shell';
import { CsIcon } from '../../shared/cs-icon/cs-icon';
import { AnonymousVisitorListItem, VisitorApiService } from './visitor-api.service';

/** Anonymous visitor records stay unlinked until an authorized operator explicitly converts them. */
@Component({
  selector: 'app-visitor-list-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, AdminShell, CsIcon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styles: [`
    :host{display:block}.wrap{padding:24px}.toolbar{display:flex;gap:12px;justify-content:space-between;align-items:end;flex-wrap:wrap}.filters{display:flex;gap:8px;flex-wrap:wrap}.grid{display:grid;grid-template-columns:minmax(0,1fr) 360px;gap:18px;margin-top:18px}.card{border:1px solid var(--cs-border);border-radius:var(--cs-radius-md);padding:16px;background:var(--cs-panel);box-shadow:var(--cs-shadow-panel)}.table-wrap{overflow:auto}table{width:100%;border-collapse:collapse;min-width:760px}th,td{text-align:left;padding:10px;border-bottom:1px solid var(--cs-border);color:var(--cs-text);font-size:13px;white-space:nowrap}th{color:var(--cs-muted);font-size:11px;text-transform:uppercase;letter-spacing:.06em;background:var(--cs-panel-raised)}input,select,button{min-height:40px;padding:8px;border:1px solid var(--cs-border-strong);border-radius:var(--cs-radius-sm);background:var(--cs-panel);color:var(--cs-text)}button{cursor:pointer}button:disabled{cursor:not-allowed;opacity:.5}form{display:grid;gap:9px}.icon-actions{display:flex;gap:6px;align-items:center}.icon-action{display:inline-grid;place-items:center;width:32px;min-height:32px;padding:6px;color:var(--cs-primary);background:var(--cs-panel-raised)}.icon-action.danger{color:var(--cs-danger)}.icon-action:hover:not(:disabled){background:var(--cs-panel-hover);border-color:var(--cs-primary)}.status{display:inline-flex;align-items:center;min-height:22px;padding:2px 8px;border-radius:999px;color:var(--cs-success);background:color-mix(in srgb,var(--cs-success) 14%,transparent);font-size:11px;font-weight:700}.status-inactive{color:var(--cs-muted);background:var(--cs-panel-raised)}.pager{display:flex;gap:8px;align-items:center;justify-content:flex-end;flex-wrap:wrap;margin-top:12px;color:var(--cs-muted);font-size:12px}.pager select{min-height:32px;padding:4px 8px}.empty{text-align:center;color:var(--cs-muted);padding:24px}.error{color:var(--cs-danger)}.ok{color:var(--cs-success)}.muted{color:var(--cs-muted)}@media(max-width:900px){.grid{grid-template-columns:1fr}.wrap{padding:16px}}
  `],
  template: `
    <app-admin-shell>
      <main class="wrap">
        <div class="toolbar">
          <div><a routerLink="/customer-admin/customers">Customers</a><h1>Anonymous Visitors</h1><p class="muted">Unknown people remain anonymous until an authorized conversion.</p></div>
          <form class="filters" [formGroup]="searchForm" (ngSubmit)="search()">
            <input formControlName="search" placeholder="Visitor code" aria-label="Search visitor code">
            <input formControlName="storeId" placeholder="Store ID" aria-label="Filter by store ID">
            <label><input type="checkbox" formControlName="activeOnly"> Active only</label>
            <button type="submit">Search</button>
          </form>
        </div>
        @if(error()){<p class="error" role="alert">{{error()}}</p>}
        @if(message()){<p class="ok" role="status">{{message()}}</p>}
        <div class="grid">
          <section class="card">
            <div class="table-wrap"><table>
              <thead><tr><th>Visitor</th><th>Store</th><th>First seen</th><th>Last seen</th><th>Status</th><th>Actions</th></tr></thead>
              <tbody>
                @for(x of visitors();track x.id){
                  <tr>
                    <td>{{x.visitorCode}}</td><td>{{x.storeId}}</td><td>{{x.firstSeenUtc | date:'short'}}</td><td>{{x.lastSeenUtc | date:'short'}}</td>
                    <td><span class="status" [class.status-inactive]="!x.isActive">{{x.convertedCustomerId?'Converted':(x.isActive?'Active':'Inactive')}}</span></td>
                    <td><div class="icon-actions">
                      @if(canConvert()&&!x.convertedCustomerId){
                        <button class="icon-action" type="button" title="Edit visitor" [attr.aria-label]="'Edit visitor '+x.visitorCode" [attr.data-testid]="'edit-visitor-'+x.id" (click)="edit(x)"><app-cs-icon name="edit" /></button>
                        <button class="icon-action" type="button" title="Convert visitor" [attr.aria-label]="'Convert '+x.visitorCode" (click)="select(x)"><app-cs-icon name="user-plus" /></button>
                        <button class="icon-action danger" type="button" title="Deactivate visitor" [attr.aria-label]="'Deactivate visitor '+x.visitorCode" [attr.data-testid]="'deactivate-visitor-'+x.id" (click)="deactivate(x)"><app-cs-icon name="trash" /></button>
                      }
                    </div></td>
                  </tr>
                } @empty {<tr><td colspan="6" class="empty">No anonymous visitors found. Try changing your filters or add a visitor.</td></tr>}
              </tbody>
            </table></div>
            <div class="pager" aria-label="Visitor pagination">
              <label>Rows <select [value]="pageSize()" (change)="changePageSize($any($event.target).value)"><option value="10">10</option><option value="25">25</option><option value="50">50</option></select></label>
              <span>Page {{pageNumber()}} of {{totalPages()}} · {{totalCount()}} records</span>
              <button type="button" [disabled]="pageNumber()<=1" (click)="previous()">Previous</button><button type="button" [disabled]="pageNumber()>=totalPages()" (click)="next()">Next</button>
            </div>
          </section>
          @if(canConvert()&&!editing()&&!selected()){<aside class="card"><h2>Add visitor</h2><p class="muted">Create an anonymous record for a store. Identity remains anonymous until explicit conversion.</p><form [formGroup]="createForm" (ngSubmit)="create()"><input type="number" formControlName="storeId" placeholder="Store ID" aria-label="Visitor store ID"><input formControlName="visitorCode" placeholder="Visitor code (optional)"><input type="datetime-local" formControlName="seenUtc" aria-label="First seen time"><button type="submit" [disabled]="createForm.invalid||saving()">Add visitor</button></form></aside>}
          @if(canConvert()&&editing();as v){<aside class="card"><h2>Edit {{v.visitorCode}}</h2><p class="muted">Deactivation preserves audit and visit history. Converted visitors remain immutable.</p><form [formGroup]="editForm" (ngSubmit)="saveEdit()"><input formControlName="visitorCode" placeholder="Visitor code"><label><input type="checkbox" formControlName="isActive"> Active</label><button type="submit" [disabled]="editForm.invalid||saving()">Save changes</button><button type="button" (click)="cancel()">Cancel</button></form></aside>}
          @if(canConvert()&&selected();as v){<aside class="card"><h2>Convert {{v.visitorCode}}</h2><p class="muted">Use an existing customer ID, or leave it blank and create a new customer below. The API rechecks customer permissions and store scope.</p><form [formGroup]="convertForm" (ngSubmit)="convert()"><input formControlName="customerId" placeholder="Existing customer ID (optional)"><input formControlName="firstName" placeholder="New customer first name"><input formControlName="lastName" placeholder="Last name"><input formControlName="mobile" placeholder="Mobile"><input formControlName="email" placeholder="Email"><input formControlName="notes" placeholder="Notes"><button type="submit" [disabled]="saving()">Convert visitor</button><button type="button" (click)="cancel()">Cancel</button></form></aside>}
        </div>
      </main>
    </app-admin-shell>
  `,
})
export class VisitorListPage implements OnInit {
  private readonly api=inject(VisitorApiService); private readonly session=inject(AuthSessionService); private readonly fb=inject(FormBuilder);
  protected readonly visitors=signal<AnonymousVisitorListItem[]>([]); protected readonly selected=signal<AnonymousVisitorListItem|null>(null); protected readonly editing=signal<AnonymousVisitorListItem|null>(null); protected readonly pageNumber=signal(1); protected readonly pageSize=signal(25); protected readonly totalCount=signal(0); protected readonly totalPages=computed(()=>Math.max(1,Math.ceil(this.totalCount()/this.pageSize()))); protected readonly saving=signal(false); protected readonly error=signal(''); protected readonly message=signal(''); protected readonly canConvert=computed(()=>this.session.hasPermission(PERMISSIONS.visitorsConvert));
  protected readonly searchForm=this.fb.nonNullable.group({search:'',storeId:'',activeOnly:true});
  protected readonly createForm=this.fb.nonNullable.group({storeId:['',Validators.required],visitorCode:'',seenUtc:''});
  protected readonly editForm=this.fb.nonNullable.group({visitorCode:['',Validators.required],isActive:true});
  protected readonly convertForm=this.fb.nonNullable.group({customerId:'',firstName:'',lastName:'',mobile:'',email:['',Validators.email],notes:''});

  ngOnInit(){this.load();}
  protected search(){this.pageNumber.set(1);this.load();}
  protected previous(){this.pageNumber.update(x=>Math.max(1,x-1));this.load();}
  protected next(){if(this.pageNumber()<this.totalPages()){this.pageNumber.update(x=>x+1);this.load();}}
  protected changePageSize(value:string){this.pageSize.set(Number(value)||25);this.pageNumber.set(1);this.load();}
  protected select(v:AnonymousVisitorListItem){this.selected.set(v);this.editing.set(null);this.convertForm.reset({customerId:'',firstName:'',lastName:'',mobile:'',email:'',notes:''});this.clearStatus();}
  protected edit(v:AnonymousVisitorListItem){this.editing.set(v);this.selected.set(null);this.editForm.reset({visitorCode:v.visitorCode,isActive:v.isActive});this.clearStatus();}
  protected cancel(){this.selected.set(null);this.editing.set(null);}
  protected create(){if(this.createForm.invalid)return;const v=this.createForm.getRawValue();const storeId=this.numberOrNull(v.storeId);if(!storeId){this.error.set('A valid store ID is required.');return;}this.saving.set(true);this.clearStatus();this.api.create({storeId,visitorCode:v.visitorCode.trim()||null,seenUtc:v.seenUtc?new Date(v.seenUtc).toISOString():null}).subscribe({next:()=>{this.saving.set(false);this.createForm.reset({storeId:'',visitorCode:'',seenUtc:''});this.message.set('Visitor added.');this.load(false);},error:e=>this.fail(e)});}
  protected saveEdit(){const visitor=this.editing();if(!visitor||this.editForm.invalid)return;const v=this.editForm.getRawValue();this.saving.set(true);this.clearStatus();this.api.update(visitor.id,{visitorCode:v.visitorCode.trim(),isActive:v.isActive}).subscribe({next:()=>{this.saving.set(false);this.editing.set(null);this.message.set('Visitor updated.');this.load(false);},error:e=>this.fail(e)});}
  protected deactivate(v:AnonymousVisitorListItem){if(v.convertedCustomerId)return;if(!window.confirm(`Deactivate ${v.visitorCode}? Visit history will be retained.`))return;this.saving.set(true);this.clearStatus();this.api.deactivate(v.id).subscribe({next:()=>{this.saving.set(false);this.message.set('Visitor deactivated.');if(this.visitors().length===1&&this.pageNumber()>1)this.pageNumber.update(x=>x-1);this.load(false);},error:e=>this.fail(e)});}
  protected convert(){const visitor=this.selected();if(!visitor)return;const v=this.convertForm.getRawValue();const customerId=this.numberOrNull(v.customerId);if(!customerId&&!v.firstName.trim()){this.error.set('First name is required when no existing customer ID is supplied.');return;}this.saving.set(true);this.clearStatus();this.api.convert(visitor.id,{customerId,firstName:customerId?null:v.firstName.trim(),lastName:customerId?null:(v.lastName.trim()||null),mobile:customerId?null:(v.mobile.trim()||null),email:customerId?null:(v.email.trim()||null),notes:customerId?null:(v.notes.trim()||null)}).subscribe({next:c=>{this.saving.set(false);this.message.set(`Visitor converted to ${c.customerCode}.`);this.selected.set(null);this.load(false);},error:e=>this.fail(e)});}
  private load(clear=true){if(clear)this.clearStatus();const v=this.searchForm.getRawValue();const filters:Record<string,string|number|boolean>={activeOnly:v.activeOnly};const storeId=this.numberOrNull(v.storeId);if(storeId)filters['storeId']=storeId;this.api.search({pageNumber:this.pageNumber(),pageSize:this.pageSize(),search:v.search.trim()||undefined,filters}).subscribe({next:r=>{this.visitors.set(r.data);this.totalCount.set(r.totalCount);const pages=Math.max(1,Math.ceil(r.totalCount/this.pageSize()));if(this.pageNumber()>pages)this.pageNumber.set(pages);},error:e=>this.fail(e)});}
  private numberOrNull(v:string){const n=Number(v);return Number.isInteger(n)&&n>0?n:null;}
  private clearStatus(){this.error.set('');this.message.set('');}
  private fail(e:unknown){this.saving.set(false);this.error.set(e instanceof HttpErrorResponse?(e.error?.message??e.message):'Request failed.');}
}
