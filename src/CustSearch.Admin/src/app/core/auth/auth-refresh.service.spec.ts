import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { AuthRefreshService } from './auth-refresh.service';
import { AuthSessionService } from './auth-session.service';

const validToken = (): string => {
  const payload = btoa(JSON.stringify({ exp: Math.floor(Date.now() / 1000) + 120 }))
    .replace(/=/g, '').replace(/\+/g, '-').replace(/\//g, '_');
  return `header.${payload}.signature`;
};

describe('AuthRefreshService logout', () => {
  let service: AuthRefreshService;
  let session: AuthSessionService;
  let controller: HttpTestingController;
  let navigate: ReturnType<typeof vi.spyOn>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(AuthRefreshService);
    session = TestBed.inject(AuthSessionService);
    controller = TestBed.inject(HttpTestingController);
    navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    session.setAccessToken(validToken());
  });

  afterEach(() => controller.verify());

  it('revokes the refresh cookie on the server before clearing the local session', async () => {
    const result = firstValueFrom(service.logout());
    const request = controller.expectOne('/api/auth/logout');
    expect(request.request.method).toBe('POST');
    expect(request.request.withCredentials).toBe(true);
    expect(session.accessToken()).not.toBeNull();
    request.flush(null);
    await expect(result).resolves.toBeNull();
    expect(session.accessToken()).toBeNull();
    expect(navigate).toHaveBeenCalledWith('/login');
  });

  it('still clears and redirects when server logout fails', async () => {
    const result = firstValueFrom(service.logout());
    controller.expectOne('/api/auth/logout').flush(null, { status: 503, statusText: 'Unavailable' });
    await expect(result).rejects.toBeTruthy();
    expect(session.accessToken()).toBeNull();
    expect(navigate).toHaveBeenCalledWith('/login');
  });
});
