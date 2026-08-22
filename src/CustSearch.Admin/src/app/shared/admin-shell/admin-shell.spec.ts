import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { AuthSessionService } from '../../core/auth/auth-session.service';
import { AdminShell } from './admin-shell';

/** Hosts the shell with required inputs so permission-filtered navigation can be tested directly. */
@Component({ imports: [AdminShell], template: `<app-admin-shell adminType="customer" pageTitle="Dashboard" eyebrow="Overview" />` })
class AdminShellHost {}

describe('AdminShell permission navigation', () => {
  it('shows granted navigation and omits entries without permission', () => {
    TestBed.configureTestingModule({ imports: [AdminShellHost], providers: [provideRouter([])] });
    TestBed.inject(AuthSessionService).setCurrentUser({
      userId: 2, tenantId: 4, tenantCode: 'SHOP', userName: 'admin', displayName: 'Admin',
      email: 'admin@example.test', isPlatformAdmin: false, roles: ['TenantAdmin'],
      permissions: ['TenantDashboard.View'], storeIds: [],
    });
    const fixture = TestBed.createComponent(AdminShellHost);
    fixture.detectChanges();
    const navigation = fixture.nativeElement.querySelector('nav')?.textContent ?? '';
    expect(navigation).toContain('Dashboard');
    expect(navigation).not.toContain('Customers');
    expect(fixture.nativeElement.textContent).not.toContain('Platform');
  });
});
