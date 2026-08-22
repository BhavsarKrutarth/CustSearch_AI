import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { authInterceptor } from './auth.interceptor';
import { AuthSessionService } from './auth-session.service';

const token = (expiresInSeconds: number): string => {
  const payload = btoa(JSON.stringify({ exp: Math.floor(Date.now() / 1000) + expiresInSeconds })).replace(/=/g, '').replace(/\+/g, '-').replace(/\//g, '_');
  return `header.${payload}.signature`;
};

describe('authInterceptor', () => {
  let http: HttpClient;
  let controller: HttpTestingController;
  let session: AuthSessionService;
  let router: Router;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideRouter([]), provideHttpClient(withInterceptors([authInterceptor])), provideHttpClientTesting()] });
    http = TestBed.inject(HttpClient);
    controller = TestBed.inject(HttpTestingController);
    session = TestBed.inject(AuthSessionService);
    router = TestBed.inject(Router);
  });
  afterEach(() => controller.verify());

  it('attaches the in-memory bearer token', async () => {
    const accessToken = token(120);
    session.setAccessToken(accessToken);
    const result = firstValueFrom(http.get('/api/data'));
    const request = controller.expectOne('/api/data');
    expect(request.request.headers.get('Authorization')).toBe(`Bearer ${accessToken}`);
    request.flush({ ok: true });
    await expect(result).resolves.toEqual({ ok: true });
  });

  it('never adds bearer credentials or refresh behavior to authentication endpoints', async () => {
    session.setAccessToken(token(120));
    const result = firstValueFrom(http.post('/api/auth/login', {}));
    const request = controller.expectOne('/api/auth/login');
    expect(request.request.headers.has('Authorization')).toBe(false);
    request.flush(null, { status: 401, statusText: 'Unauthorized' });
    await expect(result).rejects.toBeTruthy();
    controller.expectNone('/api/auth/refresh');
  });

  it('uses one refresh for concurrent 401 responses and retries each request once', async () => {
    session.setAccessToken(token(120));
    const first = firstValueFrom(http.get('/api/first'));
    const second = firstValueFrom(http.get('/api/second'));
    controller.expectOne('/api/first').flush(null, { status: 401, statusText: 'Unauthorized' });
    controller.expectOne('/api/second').flush(null, { status: 401, statusText: 'Unauthorized' });

    const refresh = controller.expectOne('/api/auth/refresh');
    expect(refresh.request.withCredentials).toBe(true);
    const renewed = token(180);
    refresh.flush({ accessToken: renewed });

    const firstRetry = controller.expectOne('/api/first');
    const secondRetry = controller.expectOne('/api/second');
    expect(firstRetry.request.headers.get('Authorization')).toBe(`Bearer ${renewed}`);
    expect(secondRetry.request.headers.get('Authorization')).toBe(`Bearer ${renewed}`);
    firstRetry.flush({ id: 1 });
    secondRetry.flush({ id: 2 });
    await expect(Promise.all([first, second])).resolves.toEqual([{ id: 1 }, { id: 2 }]);
  });

  it('clears the session and redirects when refresh fails', async () => {
    session.setAccessToken(token(120));
    const navigate = vi.spyOn(router, 'navigateByUrl').mockResolvedValue(true);
    const result = firstValueFrom(http.get('/api/private'));
    controller.expectOne('/api/private').flush(null, { status: 401, statusText: 'Unauthorized' });
    controller.expectOne('/api/auth/refresh').flush(null, { status: 401, statusText: 'Unauthorized' });
    await expect(result).rejects.toBeTruthy();
    expect(session.accessToken()).toBeNull();
    expect(navigate).toHaveBeenCalledWith('/login');
  });

  it('refreshes proactively when the access token is near expiry', async () => {
    session.setAccessToken(token(10));
    const result = firstValueFrom(http.get('/api/proactive'));
    controller.expectOne('/api/auth/refresh').flush({ accessToken: token(180) });
    controller.expectOne('/api/proactive').flush({ ok: true });
    await expect(result).resolves.toEqual({ ok: true });
  });

  it('routes a backend 403 to the access-denied page without refreshing', async () => {
    session.setAccessToken(token(120));
    const navigate = vi.spyOn(router, 'navigateByUrl').mockResolvedValue(true);
    const result = firstValueFrom(http.get('/api/restricted'));
    controller.expectOne('/api/restricted').flush(null, { status: 403, statusText: 'Forbidden' });
    await expect(result).rejects.toBeTruthy();
    expect(navigate).toHaveBeenCalledWith('/access-denied');
    controller.expectNone('/api/auth/refresh');
  });
});
