import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { TenantApiClient } from '../../core/api/tenant-api.client';
import { PageQuery, PageResponse } from '../../core/auth/auth.models';
import { CustomerDetail } from '../customers/customer-api.service';

export interface AnonymousVisitorListItem { id:number; visitorCode:string; storeId:number; firstSeenUtc:string; lastSeenUtc:string; isActive:boolean; convertedCustomerId:number|null; convertedUtc:string|null; }
export interface AnonymousVisitorDetail extends AnonymousVisitorListItem { createdUtc:string; updatedUtc:string; }
export interface ConvertVisitorRequest { customerId:number|null; firstName:string|null; lastName:string|null; mobile:string|null; email:string|null; notes:string|null; }

/** Phase 6F typed Angular client for store-scoped anonymous visitor list/detail/conversion operations. */
@Injectable({ providedIn:'root' })
export class VisitorApiService {
  private readonly api=inject(TenantApiClient);
  search(query:PageQuery):Observable<PageResponse<AnonymousVisitorListItem>>{ return this.api.getPage<AnonymousVisitorListItem>('visitors',query); }
  get(id:number):Observable<AnonymousVisitorDetail>{ return this.api.get<AnonymousVisitorDetail>(`visitors/${id}`); }
  convert(id:number,body:ConvertVisitorRequest):Observable<CustomerDetail>{ return this.api.post<CustomerDetail>(`visitors/${id}/convert`,body); }
}
