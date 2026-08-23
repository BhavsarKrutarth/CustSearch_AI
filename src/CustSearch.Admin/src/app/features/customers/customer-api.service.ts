import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { TenantApiClient } from '../../core/api/tenant-api.client';
import { PageQuery, PageResponse } from '../../core/auth/auth.models';

export interface CustomerListItem { id:number; customerCode:string; firstName:string; lastName:string|null; mobile:string|null; email:string|null; isActive:boolean; storeIds:number[]; updatedUtc:string; }
export interface CustomerDetail extends CustomerListItem { notes:string|null; primaryStoreId:number|null; createdUtc:string; }
export interface CustomerSmartProfile { customer:CustomerDetail; convertedAnonymousVisitorCount:number; lastAnonymousVisitorSeenUtc:string|null; hasMobile:boolean; hasEmail:boolean; availableSections:string[]; plannedEnrichmentSections:string[]; }
export interface CreateCustomerRequest { customerCode:string|null; firstName:string; lastName:string|null; mobile:string|null; email:string|null; notes:string|null; storeIds:number[]; primaryStoreId:number|null; }
export interface UpdateCustomerRequest { firstName:string; lastName:string|null; mobile:string|null; email:string|null; notes:string|null; isActive:boolean; }

/** Phase 6E typed Angular client for tenant-scoped customer search, CRUD, stores and smart profiles. */
@Injectable({ providedIn:'root' })
export class CustomerApiService {
  private readonly api=inject(TenantApiClient);

  search(query:PageQuery):Observable<PageResponse<CustomerListItem>>{ return this.api.getPage<CustomerListItem>('customers',query); }
  get(id:number):Observable<CustomerDetail>{ return this.api.get<CustomerDetail>(`customers/${id}`); }
  smartProfile(id:number):Observable<CustomerSmartProfile>{ return this.api.get<CustomerSmartProfile>(`customers/${id}/smart-profile`); }
  create(body:CreateCustomerRequest):Observable<CustomerDetail>{ return this.api.post<CustomerDetail>('customers',body); }
  update(id:number,body:UpdateCustomerRequest):Observable<CustomerDetail>{ return this.api.put<CustomerDetail>(`customers/${id}`,body); }
  setStores(id:number,storeIds:number[],primaryStoreId:number|null):Observable<CustomerDetail>{ return this.api.put<CustomerDetail>(`customers/${id}/stores`,{storeIds,primaryStoreId}); }
}
