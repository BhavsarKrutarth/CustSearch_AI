import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { CustomerApiService } from './customer-api.service';

/** Verifies Phase 6 customer client routing and that TenantId never leaves the browser. */
describe('CustomerApiService', () => {
  beforeEach(() => TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] }));

  afterEach(() => TestBed.inject(HttpTestingController).verify());

  it('searches the tenant customer endpoint with paging and authorized store filter only', async () => {
    const api = TestBed.inject(CustomerApiService);
    const http = TestBed.inject(HttpTestingController);
    const result = firstValueFrom(api.search({ pageNumber: 2, pageSize: 25, search: 'Priya', filters: { storeId: 101, activeOnly: true } }));

    const request = http.expectOne(item => item.url === '/api/tenant/customers');
    expect(request.request.params.get('pageNumber')).toBe('2');
    expect(request.request.params.get('pageSize')).toBe('25');
    expect(request.request.params.get('search')).toBe('Priya');
    expect(request.request.params.get('storeId')).toBe('101');
    expect(request.request.params.get('activeOnly')).toBe('true');
    expect(request.request.params.has('tenantId')).toBe(false);
    request.flush({ data: [], pageNumber: 2, pageSize: 25, totalCount: 0, totalPages: 0 });

    await expect(result).resolves.toMatchObject({ totalCount: 0 });
  });

  it('updates store visibility without a TenantId payload', async () => {
    const api = TestBed.inject(CustomerApiService);
    const http = TestBed.inject(HttpTestingController);
    const result = firstValueFrom(api.setStores(901, [101, 102], 101));
    const request = http.expectOne('/api/tenant/customers/901/stores');

    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({ storeIds: [101, 102], primaryStoreId: 101 });
    expect(request.request.body).not.toHaveProperty('tenantId');
    request.flush({ id: 901, customerCode: 'CUST-001', firstName: 'Priya', lastName: null, mobile: null, email: null, notes: null, isActive: true, storeIds: [101, 102], primaryStoreId: 101, createdUtc: '2026-08-23T00:00:00Z', updatedUtc: '2026-08-23T00:00:00Z' });

    await expect(result).resolves.toMatchObject({ primaryStoreId: 101 });
  });
});
