import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { AuthSessionService } from '../../core/auth/auth-session.service';
import { ChangePasswordPage } from './change-password-page';

const token = (): string => `header.${btoa(JSON.stringify({ exp: Math.floor(Date.now()/1000)+120 })).replace(/=/g,'')}.signature`;

describe('ChangePasswordPage', () => {
  it('submits matching strong passwords, clears memory session, and returns to login', async () => {
    await TestBed.configureTestingModule({
      imports: [ChangePasswordPage],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    const session = TestBed.inject(AuthSessionService);
    session.setAccessToken(token());
    session.setCurrentUser({ userId: 7, tenantId: 4, tenantCode: 'SHOP', userName: 'owner', displayName: 'Owner', email: 'owner@example.test', isPlatformAdmin: false, roles: ['TenantAdmin'], permissions: [], storeIds: [2] });
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    const controller = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(ChangePasswordPage);
    fixture.detectChanges();
    const inputs = fixture.nativeElement.querySelectorAll('.password-card input') as NodeListOf<HTMLInputElement>;
    for (const [input, value] of Array.from(inputs).map((input, index) => [input, ['CurrentPassword1', 'NewPassword123', 'NewPassword123'][index]] as const)) {
      input.value = value;
      input.dispatchEvent(new Event('input'));
    }
    fixture.detectChanges();
    fixture.nativeElement.querySelector('form').dispatchEvent(new Event('submit'));

    const request = controller.expectOne('/api/auth/change-password');
    expect(request.request.body).toEqual({ currentPassword: 'CurrentPassword1', newPassword: 'NewPassword123', confirmNewPassword: 'NewPassword123' });
    request.flush(null);
    fixture.detectChanges();

    expect(session.accessToken()).toBeNull();
    expect(navigate).toHaveBeenCalledWith(['/login'], { queryParams: { passwordChanged: '1' } });
    controller.verify();
  });

  it('blocks mismatched confirmation before sending a request', async () => {
    await TestBed.configureTestingModule({
      imports: [ChangePasswordPage],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    const fixture = TestBed.createComponent(ChangePasswordPage);
    fixture.detectChanges();
    const inputs = fixture.nativeElement.querySelectorAll('.password-card input') as NodeListOf<HTMLInputElement>;
    ['CurrentPassword1', 'NewPassword123', 'OtherPassword123'].forEach((value, index) => {
      inputs[index].value = value;
      inputs[index].dispatchEvent(new Event('input'));
    });
    fixture.detectChanges();
    fixture.nativeElement.querySelector('form').dispatchEvent(new Event('submit'));
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('do not match');
    TestBed.inject(HttpTestingController).expectNone('/api/auth/change-password');
  });
});
