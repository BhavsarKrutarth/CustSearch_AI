import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { ReportsApiService } from './reports-api.service';

describe('ReportsApiService',()=>{
  let api:ReportsApiService;let http:HttpTestingController;
  beforeEach(()=>{TestBed.configureTestingModule({providers:[provideHttpClient(),provideHttpClientTesting()]});api=TestBed.inject(ReportsApiService);http=TestBed.inject(HttpTestingController);});afterEach(()=>http.verify());
  it('omits TenantId from tenant export requests',async()=>{const result=firstValueFrom(api.queue(false,'Tenant.DailyVisitors',1,{storeId:7,fromUtc:'2026-08-01T00:00:00Z'}));const request=http.expectOne('/api/tenant/reports/exports');expect(request.request.method).toBe('POST');expect(request.request.body.tenantId).toBeUndefined();expect(request.request.body.storeId).toBe(7);request.flush({id:1});await result;});
  it('uses the separate platform route and supports an explicit platform tenant filter',async()=>{const result=firstValueFrom(api.queue(true,'Platform.TenantOperationalSummary',2,{tenantId:9}));const request=http.expectOne('/api/platform/reports/exports');expect(request.request.body.tenantId).toBe(9);expect(request.request.body.storeId).toBeNull();request.flush({id:2});await result;});
  it('requests protected downloads as blobs',async()=>{const result=firstValueFrom(api.download(false,42));const request=http.expectOne('/api/tenant/reports/exports/42/download');expect(request.request.responseType).toBe('blob');request.flush(new Blob(['report']));await result;});
});
