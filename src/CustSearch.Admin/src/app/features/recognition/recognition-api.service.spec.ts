import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { firstValueFrom } from 'rxjs';
import { RecognitionApiService } from './recognition-api.service';

describe('RecognitionApiService',()=>{
  let api:RecognitionApiService;let http:HttpTestingController;
  beforeEach(()=>{TestBed.configureTestingModule({providers:[provideHttpClient(),provideHttpClientTesting()]});api=TestBed.inject(RecognitionApiService);http=TestBed.inject(HttpTestingController);});
  afterEach(()=>http.verify());
  it('never accepts browser TenantId when granting purpose consent',async()=>{const result=firstValueFrom(api.grantConsent(7,{consentType:1,purpose:'Store welcome',grantedUtc:'2026-08-25T00:00:00Z',expiresUtc:null,consentVersion:'2026-01',evidenceReference:'consent:7'}));const req=http.expectOne('/api/tenant/recognition/customers/7/consents');expect(req.request.method).toBe('POST');expect(req.request.body.tenantId).toBeUndefined();req.flush({id:1});await result;});
  it('sends derived template only to the explicit customer enrollment route',async()=>{const result=firstValueFrom(api.enroll(7,{storeId:3,consentId:1,purpose:'Store welcome',derivedTemplateBase64:'ZGVyaXZlZA==',templateVersion:'onnx-v1'}));const req=http.expectOne('/api/tenant/recognition/customers/7/templates');expect(req.request.body.rawImage).toBeUndefined();expect(req.request.body.faceImage).toBeUndefined();req.flush({id:2});await result;});
  it('reviews a candidate without issuing any customer merge request',async()=>{const result=firstValueFrom(api.review(11,true,'Operator verified context'));const req=http.expectOne('/api/tenant/recognition/candidates/11/review');expect(req.request.body).toEqual({accept:true,reason:'Operator verified context'});req.flush({id:11,status:3});await result;});
});
