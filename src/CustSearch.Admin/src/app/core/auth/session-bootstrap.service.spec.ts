import { TestBed } from '@angular/core/testing';
import { Subject, firstValueFrom, of, throwError } from 'rxjs';
import { AuthApiService } from './auth-api.service';
import { AuthRefreshService } from './auth-refresh.service';
import { CurrentSessionResponse } from './auth.models';
import { SessionBootstrapService } from './session-bootstrap.service';
import { AuthSessionService } from './auth-session.service';

describe('SessionBootstrapService', () => {
  it('shares one cookie refresh and /me load across simultaneous route checks', async () => {
    const current: CurrentSessionResponse = {
      accessTokenExpiresUtc: '2026-08-16T02:00:00Z',
      user: { userId: 1, tenantId: null, tenantCode: null, userName: 'platform', displayName: 'Platform', email: 'p@example.test', isPlatformAdmin: true, roles: ['PlatformSuperAdmin'], permissions: ['Tenants.View'], storeIds: [] },
    };
    const refreshResult = new Subject<string>();
    const refresh = { refresh: vi.fn(() => refreshResult.asObservable()) };
    const api = { loadCurrentSession: vi.fn(() => of(current)) };
    TestBed.configureTestingModule({ providers: [
      { provide: AuthRefreshService, useValue: refresh },
      { provide: AuthApiService, useValue: api },
    ] });
    const service = TestBed.inject(SessionBootstrapService);
    const results = Promise.all([
      firstValueFrom(service.ensureSession()),
      firstValueFrom(service.ensureSession()),
    ]);
    expect(refresh.refresh).toHaveBeenCalledTimes(1);
    refreshResult.next('token');
    refreshResult.complete();
    await expect(results).resolves.toEqual([true, true]);
    expect(api.loadCurrentSession).toHaveBeenCalledTimes(1);
  });

  it('returns false and clears stale authorization when /me fails', async () => {
    TestBed.configureTestingModule({ providers: [
      { provide: AuthRefreshService, useValue: { refresh: () => of('token') } },
      { provide: AuthApiService, useValue: { loadCurrentSession: () => throwError(() => new Error('unauthorized')) } },
    ] });
    const session = TestBed.inject(AuthSessionService);
    session.setCurrentUser({ userId: 1, tenantId: null, tenantCode: null, userName: 'old', displayName: 'Old', email: 'old@example.test', isPlatformAdmin: true, roles: ['PlatformSuperAdmin'], permissions: ['Tenants.View'], storeIds: [] });
    await expect(firstValueFrom(TestBed.inject(SessionBootstrapService).ensureSession())).resolves.toBe(false);
    expect(session.currentUser()).toBeNull();
  });
});
