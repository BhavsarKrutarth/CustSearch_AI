import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { TenantApiClient } from './tenant-api.client';

describe('TenantApiClient', () => {
  it('builds tenant-context routes without exposing a TenantId argument', async () => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    const client = TestBed.inject(TenantApiClient);
    const controller = TestBed.inject(HttpTestingController);
    const result = firstValueFrom(client.getPage<{ id: number }>('users', { pageNumber: 1, pageSize: 25, search: 'ana' }));
    const request = controller.expectOne(item => item.url === '/api/tenant/users');
    expect(request.request.params.get('pageNumber')).toBe('1');
    expect(request.request.params.has('tenantId')).toBe(false);
    request.flush({ data: [], pageNumber: 1, pageSize: 25, totalCount: 0 });
    await expect(result).resolves.toMatchObject({ totalCount: 0 });
    controller.verify();
  });

  it('rejects a TenantId filter before an HTTP request is created', () => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    const client = TestBed.inject(TenantApiClient);
    expect(() => client.getPage('users', { pageNumber: 1, pageSize: 25, filters: { tenantId: 99 } }))
      .toThrowError('TenantId cannot be supplied by the browser.');
    TestBed.inject(HttpTestingController).verify();
  });
});
