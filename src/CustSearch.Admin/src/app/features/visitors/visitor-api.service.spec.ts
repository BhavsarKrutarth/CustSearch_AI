import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { VisitorApiService } from './visitor-api.service';

/** Verifies Phase 6 visitor reads/conversions stay tenant-contextual and explicit. */
describe('VisitorApiService', () => {
  beforeEach(() => TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] }));

  afterEach(() => TestBed.inject(HttpTestingController).verify());

  it('searches anonymous visitors with store scope and no browser TenantId', async () => {
    const api = TestBed.inject(VisitorApiService);
    const http = TestBed.inject(HttpTestingController);
    const result = firstValueFrom(api.search({ pageNumber: 1, pageSize: 25, search: 'VIS-001', filters: { storeId: 101, activeOnly: true } }));
    const request = http.expectOne(item => item.url === '/api/tenant/visitors');

    expect(request.request.params.get('storeId')).toBe('101');
    expect(request.request.params.has('tenantId')).toBe(false);
    request.flush({ data: [], pageNumber: 1, pageSize: 25, totalCount: 0, totalPages: 0 });

    await expect(result).resolves.toMatchObject({ totalCount: 0 });
  });

  it('converts a visitor only through the explicit conversion endpoint', async () => {
    const api = TestBed.inject(VisitorApiService);
    const http = TestBed.inject(HttpTestingController);
    const body = { customerId: null, firstName: 'Neha', lastName: 'Mehta', mobile: null, email: null, notes: null };
    const result = firstValueFrom(api.convert(1001, body));
    const request = http.expectOne('/api/tenant/visitors/1001/convert');

    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(body);
    expect(request.request.body).not.toHaveProperty('tenantId');
    request.flush({ id: 903, customerCode: 'CUST-CONVERTED', firstName: 'Neha', lastName: 'Mehta', mobile: null, email: null, notes: null, isActive: true, storeIds: [101], primaryStoreId: 101, createdUtc: '2026-08-23T00:00:00Z', updatedUtc: '2026-08-23T00:00:00Z' });

    await expect(result).resolves.toMatchObject({ customerCode: 'CUST-CONVERTED' });
  });
});
