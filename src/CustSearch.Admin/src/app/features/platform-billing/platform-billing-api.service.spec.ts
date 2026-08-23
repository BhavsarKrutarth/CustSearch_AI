import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { PlatformBillingApiService } from './platform-billing-api.service';

describe('PlatformBillingApiService',()=>{
  beforeEach(()=>TestBed.configureTestingModule({providers:[provideHttpClient(),provideHttpClientTesting()]}));
  afterEach(()=>TestBed.inject(HttpTestingController).verify());

  it('tenant billing reads never send TenantId',async()=>{
    const api=TestBed.inject(PlatformBillingApiService);const http=TestBed.inject(HttpTestingController);
    const result=firstValueFrom(api.tenantSummary());
    const req=http.expectOne('/api/tenant/platform-billing');
    expect(req.request.method).toBe('GET');
    expect(req.request.params.has('tenantId')).toBe(false);
    expect(JSON.stringify(req.request.body)).not.toMatch(/tenantId/i);
    req.flush({subscription:null,currentPlan:null,maxStores:5,maxUsers:20,maxStaff:10,maxCameras:5,renewalUtc:null,latestPaymentStatus:null,invoiceCount:0});
    await expect(result).resolves.toMatchObject({maxStaff:10,invoiceCount:0});
  });

  it('platform subscription creation uses tenant route id but never duplicates TenantId in body',async()=>{
    const api=TestBed.inject(PlatformBillingApiService);const http=TestBed.inject(HttpTestingController);
    const body={planId:4,billingCycle:'Monthly',startUtc:'2026-08-23T18:00:00.000Z',useTrial:true};
    const result=firstValueFrom(api.createSubscription(25,body));
    const req=http.expectOne('/api/platform/billing/subscriptions/25');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(body);
    expect(JSON.stringify(req.request.body)).not.toMatch(/tenantId/i);
    req.flush({id:1,tenantId:25,planId:4,planCode:'PRO',planName:'Pro',billingCycle:'Monthly',status:'Trial',startUtc:body.startUtc,trialEndUtc:'2026-09-06T18:00:00.000Z',currentPeriodStartUtc:body.startUtc,currentPeriodEndUtc:'2026-09-06T18:00:00.000Z',cancelAtPeriodEnd:false,cancelledUtc:null,maxStores:5,maxUsers:20,maxStaff:10,maxCameras:5});
    await expect(result).resolves.toMatchObject({tenantId:25,status:'Trial'});
  });

  it('platform payment callback is a separate platform API and contains no retail fields',async()=>{
    const api=TestBed.inject(PlatformBillingApiService);const http=TestBed.inject(HttpTestingController);
    const body={platformInvoiceId:9,paymentMethod:'UPI',amount:999,currency:'INR',gatewayReference:'gw-9',transactionReference:'txn-9',paymentUtc:'2026-08-23T18:00:00.000Z',status:'Successful'};
    const result=firstValueFrom(api.recordPayment(body));
    const req=http.expectOne('/api/platform/billing/payments');
    expect(req.request.method).toBe('POST');
    expect(JSON.stringify(req.request.body)).not.toMatch(/retail|customerId|storeId/i);
    req.flush({id:11,tenantId:25,platformInvoiceId:9,paymentMethod:'UPI',amount:999,currency:'INR',gatewayReference:'gw-9',transactionReference:'txn-9',paymentUtc:body.paymentUtc,status:'Successful'});
    await expect(result).resolves.toMatchObject({platformInvoiceId:9,transactionReference:'txn-9'});
  });

  it('rejects invalid route identifiers before sending a request',()=>{
    const api=TestBed.inject(PlatformBillingApiService);
    expect(()=>api.renew(0)).toThrowError(/valid identifier/i);
  });
});
