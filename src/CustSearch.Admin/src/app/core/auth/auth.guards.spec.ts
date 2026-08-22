import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, Router, RouterStateSnapshot, UrlTree, provideRouter } from '@angular/router';
import { Observable, firstValueFrom, of } from 'rxjs';
import { CurrentUser } from './auth.models';
import { authGuard, permissionGuard, roleGuard } from './auth.guards';
import { AuthSessionService } from './auth-session.service';
import { SessionBootstrapService } from './session-bootstrap.service';

/** Creates a valid in-memory identity for focused route-authorization tests. */
const user = (roles: string[], permissions: string[]): CurrentUser => ({
  userId: 1, tenantId: 2, tenantCode: 'SHOP', userName: 'admin', displayName: 'Admin',
  email: 'admin@example.test', isPlatformAdmin: false, roles, permissions, storeIds: [],
});

/** Executes a functional guard inside Angular's dependency-injection context. */
const runGuard = (guard: ReturnType<typeof roleGuard> | typeof authGuard): Promise<boolean | UrlTree> => {
  const result = TestBed.runInInjectionContext(() => guard(
    {} as ActivatedRouteSnapshot,
    {} as RouterStateSnapshot,
  ));
  return firstValueFrom(result as Observable<boolean | UrlTree>);
};

describe('authorization guards', () => {
  let session: AuthSessionService;
  let bootstrapReady: boolean;

  beforeEach(() => {
    bootstrapReady = true;
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: SessionBootstrapService, useValue: { ensureSession: () => of(bootstrapReady) } },
      ],
    });
    session = TestBed.inject(AuthSessionService);
  });

  it('allows matching roles and permissions', async () => {
    session.setCurrentUser(user(['TenantAdmin'], ['TenantDashboard.View']));
    await expect(runGuard(roleGuard(['TenantAdmin']))).resolves.toBe(true);
    await expect(runGuard(permissionGuard(['TenantDashboard.View']))).resolves.toBe(true);
  });

  it('returns the access-denied tree when authorization is missing', async () => {
    session.setCurrentUser(user(['Auditor'], ['AuditLogs.View']));
    const result = await runGuard(permissionGuard(['TenantDashboard.View']));
    expect(result instanceof UrlTree).toBe(true);
    expect(TestBed.inject(Router).serializeUrl(result as UrlTree)).toBe('/access-denied');
  });

  it('denies a user whose role is not accepted', async () => {
    session.setCurrentUser(user(['Auditor'], ['TenantDashboard.View']));
    const result = await runGuard(roleGuard(['TenantAdmin']));
    expect(TestBed.inject(Router).serializeUrl(result as UrlTree)).toBe('/access-denied');
  });

  it('sends an anonymous user to login when session restoration fails', async () => {
    bootstrapReady = false;
    const result = await runGuard(authGuard);
    expect(TestBed.inject(Router).serializeUrl(result as UrlTree)).toBe('/login');
  });
});
