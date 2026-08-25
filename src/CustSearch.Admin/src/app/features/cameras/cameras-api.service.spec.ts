import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { CamerasApiService } from './cameras-api.service';

describe('CamerasApiService',()=>{
  let service:CamerasApiService;let http:HttpTestingController;
  beforeEach(()=>{TestBed.configureTestingModule({providers:[provideHttpClient(),provideHttpClientTesting()]});service=TestBed.inject(CamerasApiService);http=TestBed.inject(HttpTestingController);});
  afterEach(()=>http.verify());
  it('uses tenant-relative endpoints without browser TenantId',()=>{service.cameras(7).subscribe();const request=http.expectOne('/api/tenant/cameras?storeId=7');expect(request.request.method).toBe('GET');expect(request.request.url).not.toContain('tenantId');request.flush([]);});
  it('never sends a TenantId when creating a camera',()=>{service.create({storeId:7,cameraCode:'ENTRY',name:'Entry',rtspConfigurationReference:'vault:entry',direction:1,isActive:true}).subscribe();const request=http.expectOne('/api/tenant/cameras');expect(request.request.method).toBe('POST');expect(request.request.body.tenantId).toBeUndefined();request.flush({});});
  it('loads authoritative missed tracking sessions using a cursor',()=>{service.tracks(7,41,100).subscribe();const request=http.expectOne('/api/tenant/cameras/tracks?storeId=7&afterId=41&take=100');expect(request.request.method).toBe('GET');request.flush([]);});
});
