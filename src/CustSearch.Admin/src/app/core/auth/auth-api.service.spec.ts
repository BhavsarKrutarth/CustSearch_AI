import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { AuthApiService } from './auth-api.service';
import { CurrentSessionResponse } from './auth.models';
import { AuthSessionService } from './auth-session.service';

describe('AuthApiService', () => {
  it('loads authoritative roles and permissions from /me into memory', async () => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    const api = TestBed.inject(AuthApiService);
    const controller = TestBed.inject(HttpTestingController);
    const response: CurrentSessionResponse = {
      accessTokenExpiresUtc: '2026-08-16T02:00:00Z',
      user: { userId: 7, tenantId: 4, tenantCode: 'SHOP', userName: 'owner', displayName: 'Owner', email: 'owner@example.test', isPlatformAdmin: false, roles: ['TenantAdmin'], permissions: ['TenantDashboard.View'], storeIds: [2] },
    };
    const result = firstValueFrom(api.loadCurrentSession());
    controller.expectOne('/api/auth/me').flush(response);
    await expect(result).resolves.toEqual(response);
    expect(TestBed.inject(AuthSessionService).hasPermission('TenantDashboard.View')).toBe(true);
    controller.verify();
  });

  it('sends password changes to the secure auth boundary without persisting credentials', async () => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    const api = TestBed.inject(AuthApiService);
    const controller = TestBed.inject(HttpTestingController);

    const result = firstValueFrom(api.changePassword('CurrentPassword1', 'NewPassword123', 'NewPassword123'));
    const request = controller.expectOne('/api/auth/change-password');
    expect(request.request.method).toBe('POST');
    expect(request.request.withCredentials).toBe(true);
    expect(request.request.body).toEqual({
      currentPassword: 'CurrentPassword1',
      newPassword: 'NewPassword123',
      confirmNewPassword: 'NewPassword123',
    });
    request.flush(null);

    await expect(result).resolves.toBeNull();
    controller.verify();
  });

  it('revokes the refresh-cookie session on logout', async () => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    const api = TestBed.inject(AuthApiService);
    const controller = TestBed.inject(HttpTestingController);
    const result = firstValueFrom(api.logout());
    const request = controller.expectOne('/api/auth/logout');
    expect(request.request.method).toBe('POST');
    expect(request.request.withCredentials).toBe(true);
    request.flush(null, { status: 204, statusText: 'No Content' });
    await expect(result).resolves.toBeNull();
    controller.verify();
  });
});
