import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { TenantApiClient } from '../../core/api/tenant-api.client';
import { PageQuery, PageResponse } from '../../core/auth/auth.models';

export interface CustomerVisitListItem { id:number; visitCode:string; customerId:number; customerCode:string; customerName:string; storeId:number; visitPartyId:number|null; enteredUtc:string; exitedUtc:string|null; source:number; status:number; }
export interface CustomerVisitDetail extends CustomerVisitListItem { createdUtc:string; updatedUtc:string; }
export interface VisitPartyListItem { id:number; partyCode:string; storeId:number; startedUtc:string; endedUtc:string|null; source:number; status:number; memberCount:number; }
export interface VisitPartyMember { id:number; identityType:number; customerId:number|null; customerCode:string|null; anonymousVisitorId:number|null; visitorCode:string|null; joinedUtc:string; }
export interface VisitPartyDetail extends VisitPartyListItem { members:VisitPartyMember[]; createdUtc:string; updatedUtc:string; }
export interface CreateVisitRequest { storeId:number; customerId:number; visitPartyId:number|null; enteredUtc:string|null; }

/** Phase 7F typed factual visit and Visit Party/Co-Visit client. TenantId is never browser supplied. */
@Injectable({providedIn:'root'})
export class VisitApiService {
 private readonly api=inject(TenantApiClient);
 searchVisits(query:PageQuery):Observable<PageResponse<CustomerVisitListItem>>{return this.api.getPage<CustomerVisitListItem>('visits',query);}
 getVisit(id:number):Observable<CustomerVisitDetail>{return this.api.get<CustomerVisitDetail>(`visits/${id}`);}
 createVisit(body:CreateVisitRequest):Observable<CustomerVisitDetail>{return this.api.post<CustomerVisitDetail>('visits',body);}
 completeVisit(id:number,exitedUtc:string|null):Observable<CustomerVisitDetail>{return this.api.post<CustomerVisitDetail>(`visits/${id}/complete`,{exitedUtc});}
 searchParties(query:PageQuery):Observable<PageResponse<VisitPartyListItem>>{return this.api.getPage<VisitPartyListItem>('visit-parties',query);}
 getParty(id:number):Observable<VisitPartyDetail>{return this.api.get<VisitPartyDetail>(`visit-parties/${id}`);}
}