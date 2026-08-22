import { TestBed } from '@angular/core/testing';
import { AuthSessionService } from './auth-session.service';

const tokenWithExpiry = (expirySeconds: number): string =>
  `header.${btoa(JSON.stringify({ exp: expirySeconds })).replace(/=/g, '').replace(/\+/g, '-').replace(/\//g, '_')}.signature`;

describe('AuthSessionService', () => {
  let service: AuthSessionService;
  beforeEach(() => { TestBed.configureTestingModule({}); service = TestBed.inject(AuthSessionService); });

  it('keeps a valid access token in memory and understands expiry', () => {
    const expiry = Math.floor(Date.now() / 1000) + 120;
    expect(service.setAccessToken(tokenWithExpiry(expiry))).toBe(true);
    expect(service.accessToken()).not.toBeNull();
    expect(service.isExpired()).toBe(false);
    expect(service.isExpiringWithin(180)).toBe(true);
  });

  it('rejects malformed and expired access tokens', () => {
    expect(service.setAccessToken('invalid')).toBe(false);
    expect(service.setAccessToken(tokenWithExpiry(Math.floor(Date.now() / 1000) - 1))).toBe(false);
    expect(service.accessToken()).toBeNull();
  });

  it('never persists the token in web storage', () => {
    const localSpy = vi.spyOn(Storage.prototype, 'setItem');
    service.setAccessToken(tokenWithExpiry(Math.floor(Date.now() / 1000) + 120));
    expect(localSpy).not.toHaveBeenCalled();
  });
});
