import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { OperationsApiService } from './operations-api.service';

describe('OperationsApiService',()=>{
  let api:OperationsApiService;let http:HttpTestingController;
  beforeEach(()=>{TestBed.configureTestingModule({providers:[provideHttpClient(),provideHttpClientTesting()]});api=TestBed.inject(OperationsApiService);http=TestBed.inject(HttpTestingController);});
  afterEach(()=>http.verify());
  it('tenant settings never send a tenant id',()=>{api.settings(false,42).subscribe();const request=http.expectOne(r=>r.url==='/api/tenant/operations/settings');expect(request.request.params.get('storeId')).toBe('42');expect(request.request.params.has('tenantId')).toBe(false);request.flush([]);});
  it('platform health uses the protected operations endpoint',()=>{api.health().subscribe();http.expectOne('/api/platform/operations/health').flush({});});
});
