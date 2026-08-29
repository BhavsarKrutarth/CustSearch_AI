import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { SecurityApiService } from './security-api.service';

describe('SecurityApiService',()=>{
 let service:SecurityApiService;let http:HttpTestingController;
 beforeEach(()=>{TestBed.configureTestingModule({providers:[provideHttpClient(),provideHttpClientTesting()]});service=TestBed.inject(SecurityApiService);http=TestBed.inject(HttpTestingController);});
 afterEach(()=>http.verify());
 it('requests a server-authorized evidence ticket without tenant input',()=>{service.evidenceTicket(7,9).subscribe();const request=http.expectOne('/api/tenant/security/incidents/7/evidence/9/view-ticket');expect(request.request.method).toBe('POST');expect(request.request.body).toEqual({});request.flush({token:'signed',expiresUtc:'2026-08-28T00:00:00Z'});});
 it('builds only a same-origin encoded evidence URL',()=>{expect(service.evidenceUrl(7,9,'a+b/c=')).toBe('/api/tenant/security/incidents/7/evidence/9/view?token=a%2Bb%2Fc%3D');});
});
