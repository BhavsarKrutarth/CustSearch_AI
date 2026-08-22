import { Injectable, computed, signal } from '@angular/core';
import { CurrentUser } from './auth.models';

interface JwtPayload { exp?: number; [claim: string]: unknown }

@Injectable({ providedIn: 'root' })
/** Holds short-lived authentication and authorization state only in browser memory. */
export class AuthSessionService {
  private readonly accessTokenState = signal<string | null>(null);
  private readonly expiresAtState = signal<number | null>(null);
  private readonly currentUserState = signal<CurrentUser | null>(null);

  readonly accessToken = this.accessTokenState.asReadonly();
  readonly expiresAt = this.expiresAtState.asReadonly();
  readonly currentUser = this.currentUserState.asReadonly();
  readonly isAuthenticated = computed(() => this.accessTokenState() !== null && !this.isExpired());
  readonly roles = computed(() => this.currentUserState()?.roles ?? []);
  readonly permissions = computed(() => this.currentUserState()?.permissions ?? []);

  setAccessToken(token: string): boolean {
    const expiry = this.readExpiry(token);
    if (expiry === null || expiry <= Date.now()) {
      this.clear();
      return false;
    }
    this.accessTokenState.set(token);
    this.expiresAtState.set(expiry);
    return true;
  }

  clear(): void {
    this.accessTokenState.set(null);
    this.expiresAtState.set(null);
    this.currentUserState.set(null);
  }

  setCurrentUser(user: CurrentUser): void {
    this.currentUserState.set(user);
  }

  hasRole(role: string): boolean {
    return this.roles().includes(role);
  }

  hasPermission(permission: string): boolean {
    return this.permissions().includes(permission);
  }

  isExpired(now = Date.now()): boolean {
    const expiry = this.expiresAtState();
    return expiry === null || expiry <= now;
  }

  isExpiringWithin(seconds: number, now = Date.now()): boolean {
    const expiry = this.expiresAtState();
    return expiry !== null && expiry <= now + seconds * 1000;
  }

  private readExpiry(token: string): number | null {
    try {
      const payloadPart = token.split('.')[1];
      if (!payloadPart) return null;
      const base64 = payloadPart.replace(/-/g, '+').replace(/_/g, '/').padEnd(Math.ceil(payloadPart.length / 4) * 4, '=');
      const payload = JSON.parse(atob(base64)) as JwtPayload;
      return typeof payload.exp === 'number' && Number.isFinite(payload.exp) ? payload.exp * 1000 : null;
    } catch {
      return null;
    }
  }
}
