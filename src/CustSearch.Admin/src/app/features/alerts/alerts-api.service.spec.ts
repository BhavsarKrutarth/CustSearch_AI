import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { AlertsApiService } from './alerts-api.service';

describe('AlertsApiService',()=>{
  beforeEach(()=>TestBed.configureTestingModule({providers:[provideHttpClient(),provideHttpClientTesting()]}));
  afterEach(()=>TestBed.inject(HttpTestingController).verify());
  it('loads scoped alerts without a browser TenantId',async()=>{const api=TestBed.inject(AlertsApiService);const http=TestBed.inject(HttpTestingController);const result=firstValueFrom(api.list(7,2));const req=http.expectOne('/api/tenant/alerts?storeId=7&status=2');expect(req.request.method).toBe('GET');expect(req.request.params.has('tenantId')).toBe(false);req.flush({items:[],unreadCount:0,lastEventId:9});await expect(result).resolves.toMatchObject({lastEventId:9});});
  it('acknowledges with an empty body so identity remains server-derived',async()=>{const api=TestBed.inject(AlertsApiService);const http=TestBed.inject(HttpTestingController);const result=firstValueFrom(api.acknowledge(8));const req=http.expectOne('/api/tenant/alerts/8/acknowledge');expect(req.request.method).toBe('POST');expect(req.request.body).toEqual({});expect(JSON.stringify(req.request.body)).not.toMatch(/tenantId|userId|storeId/i);req.flush(alert(8,3));await expect(result).resolves.toMatchObject({id:8,status:3});});
  it('recovers only after the durable event cursor',async()=>{const api=TestBed.inject(AlertsApiService);const http=TestBed.inject(HttpTestingController);const result=firstValueFrom(api.recover(41,100));const req=http.expectOne('/api/tenant/alerts/recovery?afterEventId=41&take=100');expect(req.request.method).toBe('GET');req.flush({requestedAfterEventId:41,nextCursor:42,events:[]});await expect(result).resolves.toMatchObject({nextCursor:42});});
  const alert=(id:number,status:1|2|3|4|5)=>({id,alertType:'returning.customer',storeId:7,severity:2,title:'Returning customer',message:'Customer returned.',entityType:'Customer',entityId:'9',createdUtc:'2026-08-24T15:00:00Z',acknowledgedUtc:status>=3?'2026-08-24T15:01:00Z':null,acknowledgedByUserId:status>=3?2:null,resolvedUtc:status===4?'2026-08-24T15:02:00Z':null,status,correlationId:'p11-test',deduplicationKey:`alert-${id}`});
});
