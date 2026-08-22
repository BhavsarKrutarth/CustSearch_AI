import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { CurrentUser } from '../../core/auth/auth.models';
import { AuthSessionService } from '../../core/auth/auth-session.service';
import { HasPermissionDirective } from './has-permission.directive';

/** Hosts the structural directive without coupling the test to a feature page. */
@Component({ imports: [HasPermissionDirective], template: `<button *appHasPermission="'Customers.Edit'">Edit</button>` })
class PermissionHost {}

describe('HasPermissionDirective', () => {
  it('renders only after the server session grants the permission', () => {
    TestBed.configureTestingModule({ imports: [PermissionHost] });
    const fixture = TestBed.createComponent(PermissionHost);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('button')).toBeNull();
    const granted: CurrentUser = { userId: 1, tenantId: 2, tenantCode: 'SHOP', userName: 'admin', displayName: 'Admin', email: 'a@example.test', isPlatformAdmin: false, roles: ['TenantAdmin'], permissions: ['Customers.Edit'], storeIds: [] };
    TestBed.inject(AuthSessionService).setCurrentUser(granted);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('button')?.textContent).toContain('Edit');
  });
});
