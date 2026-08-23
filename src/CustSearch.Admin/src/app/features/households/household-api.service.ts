import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { TenantApiClient } from '../../core/api/tenant-api.client';
import { PageQuery, PageResponse } from '../../core/auth/auth.models';

export type HouseholdRelationshipSource = 1|2|3|4;
export interface HouseholdListItem { id:number; householdCode:string; name:string; visibleMemberCount:number; isActive:boolean; updatedUtc:string; }
export interface HouseholdMember { customerId:number; customerCode:string; firstName:string; lastName:string|null; relationshipType:string; relationshipSource:HouseholdRelationshipSource; isVerified:boolean; verifiedByUserId:number; verifiedUtc:string; isActive:boolean; }
export interface HouseholdDetail { id:number; householdCode:string; name:string; notes:string|null; isActive:boolean; members:HouseholdMember[]; createdUtc:string; updatedUtc:string; }
export interface CreateHouseholdRequest { householdCode:string|null; name:string; notes:string|null; }
export interface UpdateHouseholdRequest { name:string; notes:string|null; isActive:boolean; }
export interface SaveHouseholdMemberRequest { customerId:number; relationshipType:string; relationshipSource:HouseholdRelationshipSource; }
export interface UpdateHouseholdMemberRequest { relationshipType:string; relationshipSource:HouseholdRelationshipSource; isActive:boolean; }

/** Phase 7E typed household client. TenantId is intentionally absent from all browser contracts. */
@Injectable({providedIn:'root'})
export class HouseholdApiService {
  private readonly api=inject(TenantApiClient);
  search(query:PageQuery):Observable<PageResponse<HouseholdListItem>>{return this.api.getPage<HouseholdListItem>('households',query);}
  get(id:number):Observable<HouseholdDetail>{return this.api.get<HouseholdDetail>(`households/${id}`);}
  create(body:CreateHouseholdRequest):Observable<HouseholdDetail>{return this.api.post<HouseholdDetail>('households',body);}
  update(id:number,body:UpdateHouseholdRequest):Observable<HouseholdDetail>{return this.api.put<HouseholdDetail>(`households/${id}`,body);}
  addMember(id:number,body:SaveHouseholdMemberRequest):Observable<HouseholdDetail>{return this.api.post<HouseholdDetail>(`households/${id}/members`,body);}
  updateMember(id:number,customerId:number,body:UpdateHouseholdMemberRequest):Observable<HouseholdDetail>{return this.api.put<HouseholdDetail>(`households/${id}/members/${customerId}`,body);}
  removeMember(id:number,customerId:number):Observable<void>{return this.api.delete<void>(`households/${id}/members/${customerId}`);}
}