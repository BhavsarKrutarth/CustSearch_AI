import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { RetailApiService } from './retail-api.service';

describe('RetailApiService',()=>{
  beforeEach(()=>TestBed.configureTestingModule({providers:[provideHttpClient(),provideHttpClientTesting()]}));
  afterEach(()=>TestBed.inject(HttpTestingController).verify());

  it('searches products without browser TenantId',async()=>{
    const api=TestBed.inject(RetailApiService);const http=TestBed.inject(HttpTestingController);
    const result=firstValueFrom(api.searchProducts({pageNumber:1,pageSize:25,search:'silk',filters:{storeId:10,activeOnly:true}}));
    const req=http.expectOne(x=>x.url==='/api/tenant/products');
    expect(req.request.params.get('search')).toBe('silk');
    expect(req.request.params.get('storeId')).toBe('10');
    expect(req.request.params.has('tenantId')).toBe(false);
    req.flush({data:[],pageNumber:1,pageSize:25,totalCount:0,totalPages:0});
    await expect(result).resolves.toMatchObject({totalCount:0});
  });

  it('creates invoice with business fields only and never sends TenantId',async()=>{
    const api=TestBed.inject(RetailApiService);const http=TestBed.inject(HttpTestingController);
    const body={storeId:10,customerId:501,householdId:null,customerVisitId:null,visitPartyId:null,notes:null,items:[{productId:20,quantity:2,discountAmount:100}]};
    const result=firstValueFrom(api.createInvoice(body));
    const req=http.expectOne('/api/tenant/retail/invoices');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(body);
    expect(JSON.stringify(req.request.body)).not.toMatch(/tenantId/i);
    req.flush({id:1,invoiceNumber:'INV-1',storeId:10,customerId:501,customerCode:'C-1',customerName:'Test',invoiceUtc:'2026-08-23T00:00:00Z',grandTotal:1995,paidAmount:0,balanceAmount:1995,status:1,householdId:null,customerVisitId:null,visitPartyId:null,subtotal:2000,discountAmount:100,taxAmount:95,notes:null,items:[],payments:[],participants:[],attributions:[],attributedTotal:0,unattributedTotal:1995,createdUtc:'2026-08-23T00:00:00Z',updatedUtc:'2026-08-23T00:00:00Z',cancelledUtc:null,cancellationReason:null});
    await expect(result).resolves.toMatchObject({grandTotal:1995});
  });

  it('keeps household purchase summary and customer history as separate factual APIs',async()=>{
    const api=TestBed.inject(RetailApiService);const http=TestBed.inject(HttpTestingController);
    const history=firstValueFrom(api.customerPurchaseHistory(501));
    const hreq=http.expectOne('/api/tenant/customers/501/purchase-history');hreq.flush({customerId:501,invoiceCount:0,payerSpend:0,explicitAttributedSpend:0,lastPurchaseUtc:null,lastPurchaseStoreId:null,recentInvoices:[]});
    await expect(history).resolves.toMatchObject({explicitAttributedSpend:0});
    const household=firstValueFrom(api.householdPurchaseSummary(44));
    const freq=http.expectOne('/api/tenant/households/44/purchase-summary');freq.flush({householdId:44,invoiceCount:0,verifiedMemberAttributedSpend:0,lastPurchaseUtc:null});
    await expect(household).resolves.toMatchObject({verifiedMemberAttributedSpend:0});
  });
});
