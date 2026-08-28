import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { CamerasApiService } from './cameras-api.service';

describe('CamerasApiService',()=>{
  let service:CamerasApiService;let http:HttpTestingController;
  beforeEach(()=>{TestBed.configureTestingModule({providers:[provideHttpClient(),provideHttpClientTesting()]});service=TestBed.inject(CamerasApiService);http=TestBed.inject(HttpTestingController);});
  afterEach(()=>http.verify());
  it('uses tenant-relative endpoints without browser TenantId',()=>{service.cameras(7).subscribe();const request=http.expectOne('/api/tenant/cameras?storeId=7');expect(request.request.method).toBe('GET');expect(request.request.url).not.toContain('tenantId');request.flush([]);});
  it('loads the server-derived tenant camera quota',()=>{service.quota().subscribe();const request=http.expectOne('/api/tenant/cameras/quota');expect(request.request.method).toBe('GET');expect(request.request.url).not.toContain('tenantId');request.flush({maxCameras:5,configuredCameras:5,activeCameras:5,availableCameras:0,canAddActiveCamera:false});});
  it('saves motion settings without a client TenantId',()=>{service.saveMotionSettings(31,true).subscribe();const request=http.expectOne('/api/tenant/cameras/31/motion-settings');expect(request.request.method).toBe('PUT');expect(request.request.body).toEqual({enabled:true});expect(request.request.body.tenantId).toBeUndefined();request.flush({cameraId:31,motionRulesEnabled:true});});
  it('never sends a TenantId when creating a camera',()=>{service.create({storeId:7,cameraCode:'ENTRY',name:'Entry',rtspConfigurationReference:'vault:entry',direction:1,isActive:true}).subscribe();const request=http.expectOne('/api/tenant/cameras');expect(request.request.method).toBe('POST');expect(request.request.body.tenantId).toBeUndefined();request.flush({});});
  it('loads authoritative missed tracking sessions using a cursor',()=>{service.tracks(7,41,100).subscribe();const request=http.expectOne('/api/tenant/cameras/tracks?storeId=7&afterId=41&take=100');expect(request.request.method).toBe('GET');request.flush([]);});
  it('creates user-camera preview grants without a browser TenantId',()=>{service.savePreviewGrant(31,813,{canViewLive:true,canViewTracking:true,canControl:false,validUntilUtc:null,isActive:true}).subscribe();const request=http.expectOne('/api/tenant/cameras/31/preview-grants/813');expect(request.request.method).toBe('PUT');expect(request.request.body.tenantId).toBeUndefined();request.flush({});});
  it('loads preview frames as authenticated blobs through the tenant API',()=>{service.previewFrame(31,'session-1').subscribe();const request=http.expectOne('/api/tenant/cameras/31/preview-sessions/session-1/frame');expect(request.request.method).toBe('GET');expect(request.request.responseType).toBe('blob');request.flush(new Blob(['frame'],{type:'image/jpeg'}));});
});
