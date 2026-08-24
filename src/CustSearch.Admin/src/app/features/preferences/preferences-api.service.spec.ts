import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { PreferencesApiService } from './preferences-api.service';

describe('PreferencesApiService',()=>{
  beforeEach(()=>TestBed.configureTestingModule({providers:[provideHttpClient(),provideHttpClientTesting()]}));
  afterEach(()=>TestBed.inject(HttpTestingController).verify());

  it('customer preference reads never send TenantId',async()=>{
    const api=TestBed.inject(PreferencesApiService);const http=TestBed.inject(HttpTestingController);const result=firstValueFrom(api.customer(7));const req=http.expectOne('/api/tenant/customers/7/preferences');expect(req.request.method).toBe('GET');expect(req.request.params.has('tenantId')).toBe(false);req.flush({customerId:7,customerCode:'C7',customerName:'Customer 7',signals:[],scores:[]});await expect(result).resolves.toMatchObject({customerId:7});
  });

  it('voice interpret sends observation plus optional server candidate only',async()=>{
    const api=TestBed.inject(PreferencesApiService);const http=TestBed.inject(HttpTestingController);const body={recognizedText:'Banarasi Sadi',recognitionConfidence:93,selectedCategoryId:null,reason:'voice test'};const result=firstValueFrom(api.interpretVoice(5,body));const req=http.expectOne('/api/tenant/voice/commands/5/interpret');expect(req.request.method).toBe('POST');expect(req.request.body).toEqual(body);const json=JSON.stringify(req.request.body);expect(json).not.toMatch(/tenantId|preferenceType|referenceId|\"value\"|customerId|storeId/i);req.flush({session:{id:5,storeId:2,customerId:7,matchedTrigger:'Magic Add',recognizedText:null,recognitionConfidence:null,proposedPreferenceType:null,proposedReferenceId:null,proposedValue:null,confirmationRequired:true,status:1,expiresUtc:'2026-08-24T03:00:30Z',resolvedUtc:null},needsCategorySelection:true,candidates:[{categoryId:11,categoryCode:'BAN',categoryName:'Banarasi Saree',matchSource:'Alias'}],resolutionMessage:'Select category'});await expect(result).resolves.toMatchObject({needsCategorySelection:true});
  });

  it('category alias maps to existing category endpoint without TenantId',async()=>{
    const api=TestBed.inject(PreferencesApiService);const http=TestBed.inject(HttpTestingController);const body={storeId:2,aliasText:'Banarasi Sadi',languageCode:'gu-IN'};const result=firstValueFrom(api.addCategoryAlias(11,body));const req=http.expectOne('/api/tenant/store-categories/11/aliases');expect(req.request.method).toBe('POST');expect(JSON.stringify(req.request.body)).not.toMatch(/tenantId/i);req.flush({id:1,storeId:2,productCategoryId:11,aliasText:body.aliasText,normalizedAliasText:'banarasi sadi',languageCode:'gu-IN',isActive:true,createdUtc:'2026-08-24T03:00:00Z'});await expect(result).resolves.toMatchObject({productCategoryId:11});
  });

  it('voice runtime settings remain store scoped and configurable',async()=>{
    const api=TestBed.inject(PreferencesApiService);const http=TestBed.inject(HttpTestingController);const result=firstValueFrom(api.voiceSetting(3));const req=http.expectOne('/api/tenant/stores/3/voice-command-runtime');expect(req.request.method).toBe('GET');req.flush({storeId:3,triggerKeyword:'Smart Add',responseMode:'InAppAndVoice',isEnabled:true,requireConfirmationForAmbiguousCategory:true,aliases:['Apna Add'],languageCode:'hi-IN',requireConfirmation:true,listeningTimeoutSeconds:30,minimumRecognitionConfidence:70});await expect(result).resolves.toMatchObject({triggerKeyword:'Smart Add'});
  });

  it('audit route remains tenant scoped',async()=>{
    const api=TestBed.inject(PreferencesApiService);const http=TestBed.inject(HttpTestingController);const result=firstValueFrom(api.audit(undefined,4));const req=http.expectOne('/api/tenant/preferences/audit?storeId=4');expect(req.request.method).toBe('GET');expect(req.request.params.has('tenantId')).toBe(false);req.flush([]);await expect(result).resolves.toEqual([]);
  });
});
