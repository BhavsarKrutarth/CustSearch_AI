import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { AssignTenantSubscriptionRequest, CreateTenantRequest, PageResult, PlatformDashboardSummary, PlatformStoreListItem, PlatformTenantAdministrator, PlatformTenantDetail, PlatformTenantListItem, PlatformTenantSummary, PlatformTenantUserListItem, ResetTenantAdministratorPasswordRequest, SaveSubscriptionPlanRequest, SubscriptionPlanOption, TenantAuditItem, TenantListQuery, TenantUsageItem, UpdateTenantRequest } from './platform-tenant.models';

/** Centralizes typed same-origin platform calls; the server remains the tenant and permission authority. */
@Injectable({ providedIn: 'root' })
export class PlatformTenantApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/platform/tenants';
  dashboard() { return this.http.get<PlatformDashboardSummary>('/api/platform/dashboard'); }

  list(query: TenantListQuery): Observable<PageResult<PlatformTenantListItem>> {
    let params = new HttpParams().set('page', query.page).set('pageSize', query.pageSize);
    if (query.search) params = params.set('search', query.search);
    if (query.status) params = params.set('status', query.status);
    if (query.planId) params = params.set('planId', query.planId);
    return this.http.get<PageResult<PlatformTenantListItem>>(this.baseUrl, { params });
  }
  tenantUsers(page=1,pageSize=25,search='') { return this.http.get<PageResult<PlatformTenantUserListItem>>('/api/platform/tenant-users',{params:{page,pageSize,...(search?{search}:{})}}); }
  stores(page=1,pageSize=25,search='') { return this.http.get<PageResult<PlatformStoreListItem>>('/api/platform/stores',{params:{page,pageSize,...(search?{search}:{})}}); }
  get(id: number) { return this.http.get<PlatformTenantDetail>(`${this.baseUrl}/${this.validId(id)}`); }
  create(request: CreateTenantRequest) { return this.http.post<PlatformTenantDetail>(this.baseUrl, request); }
  update(id: number, request: UpdateTenantRequest) { return this.http.put<PlatformTenantDetail>(`${this.baseUrl}/${this.validId(id)}`, request); }
  administrator(id:number){return this.http.get<PlatformTenantAdministrator>(`${this.baseUrl}/${this.validId(id)}/administrator`);}
  resetAdministratorPassword(id:number,request:ResetTenantAdministratorPasswordRequest){return this.http.put<PlatformTenantAdministrator>(`${this.baseUrl}/${this.validId(id)}/administrator/password`,request);}
  activate(id: number, expectedVersion: string) { return this.http.post<PlatformTenantDetail>(`${this.baseUrl}/${this.validId(id)}/activate`, { expectedVersion, reason: null }); }
  suspend(id: number, reason: string, expectedVersion: string) { return this.http.post<PlatformTenantDetail>(`${this.baseUrl}/${this.validId(id)}/suspend`, { expectedVersion, reason }); }
  summary(id: number) { return this.http.get<PlatformTenantSummary>(`${this.baseUrl}/${this.validId(id)}/summary`); }
  usage(id: number) { return this.http.get<TenantUsageItem[]>(`${this.baseUrl}/${this.validId(id)}/usage`); }
  audit(id: number, page = 1, pageSize = 10) { return this.http.get<PageResult<TenantAuditItem>>(`${this.baseUrl}/${this.validId(id)}/audit`, { params: { page, pageSize } }); }
  assignSubscription(id: number, request: AssignTenantSubscriptionRequest) { return this.http.put<PlatformTenantDetail>(`${this.baseUrl}/${this.validId(id)}/subscription`, request); }
  plans() { return this.http.get<SubscriptionPlanOption[]>('/api/platform/subscription-plans'); }
  createPlan(request: SaveSubscriptionPlanRequest) { return this.http.post<SubscriptionPlanOption>('/api/platform/subscription-plans',request); }
  updatePlan(id:number,request:SaveSubscriptionPlanRequest) { return this.http.put<SubscriptionPlanOption>(`/api/platform/subscription-plans/${this.validId(id)}`,request); }

  private validId(id: number): number {
    if (!Number.isSafeInteger(id) || id <= 0) throw new Error('A valid tenant identifier is required.');
    return id;
  }
}
