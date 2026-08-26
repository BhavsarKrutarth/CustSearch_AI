import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { PhaseFiveApiService, TenantUser } from './phase-five-api.service';

describe('PhaseFiveApiService password reset', () => {
  it('uses the tenant-relative user password route without a client TenantId', async () => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    const api = TestBed.inject(PhaseFiveApiService);
    const controller = TestBed.inject(HttpTestingController);
    const result = firstValueFrom(api.resetUserPassword(22, 'ResetPassword123', 'ResetPassword123'));
    const request = controller.expectOne('/api/tenant/users/22/password');
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({ newPassword: 'ResetPassword123', confirmNewPassword: 'ResetPassword123' });
    expect(JSON.stringify(request.request.body)).not.toContain('tenantId');
    request.flush({ id: 22, userName: 'staff', email: 'staff@example.test', displayName: 'Staff', isActive: true, roles: ['Staff'], storeIds: [3], createdUtc: '2026-08-26T00:00:00Z' } satisfies TenantUser);

    await expect(result).resolves.toMatchObject({ id: 22 });
    controller.verify();
  });
});
