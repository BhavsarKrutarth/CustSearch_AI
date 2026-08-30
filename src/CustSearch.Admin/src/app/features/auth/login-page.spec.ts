import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { authInterceptor } from '../../core/auth/auth.interceptor';
import { AuthSessionService } from '../../core/auth/auth-session.service';
import { LoginPage } from './login-page';

const token = (): string => `header.${btoa(JSON.stringify({ exp: Math.floor(Date.now()/1000)+120 })).replace(/=/g,'')}.signature`;

describe('LoginPage', () => {
  it('signs in with credentials, keeps the token in memory, and routes by admin type', async () => {
    await TestBed.configureTestingModule({
      imports: [LoginPage],
      providers: [provideRouter([]), provideHttpClient(withInterceptors([authInterceptor])), provideHttpClientTesting()],
    }).compileComponents();
    const controller = TestBed.inject(HttpTestingController);
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    const fixture = TestBed.createComponent(LoginPage);
    fixture.detectChanges();
    const page = fixture.componentInstance as unknown as { username: string; password: string; signIn(): void };
    page.username = 'admin@example.com';
    page.password = 'secret';
    page.signIn();
    const request = controller.expectOne('/api/auth/login');
    expect(request.request.withCredentials).toBe(true);
    expect(request.request.headers.has('Authorization')).toBe(false);
    request.flush({ accessToken: token(), user: { isPlatformAdmin: true } });
    fixture.detectChanges();
    expect(TestBed.inject(AuthSessionService).accessToken()).not.toBeNull();
    expect(navigate).toHaveBeenCalledWith('/platform-admin');
    controller.verify();
  });
});
