import { Routes } from '@angular/router';
import { authGuard, permissionGuard, roleGuard } from './core/auth/auth.guards';
import { PERMISSIONS } from './core/auth/permission-catalog';

/** Defines the platform roles accepted by the cross-tenant admin shell. */
const platformRoles = ['PlatformSuperAdmin', 'PlatformOperationsAdmin', 'PlatformBillingAdmin', 'PlatformSupportAdmin', 'PlatformAuditor'];

/** Defines the tenant roles accepted by the tenant-scoped Customer Admin shell. */
const tenantRoles = ['TenantAdmin', 'StoreAdmin', 'Manager', 'CRMStaff', 'BillingStaff', 'CameraOperator', 'IntegrationAdmin', 'Auditor'];

export const routes: Routes = [
  {
    path: 'login',
    title: 'Sign in | CustSearch AI',
    loadComponent: () => import('./features/auth/login-page').then(m => m.LoginPage),
  },
  { path: '', pathMatch: 'full', redirectTo: 'login' },
  {
    path: 'access-denied',
    title: 'Access denied | CustSearch AI',
    loadComponent: () => import('./features/auth/access-denied-page').then(m => m.AccessDeniedPage),
  },
  {
    path: 'customer-admin',
    title: 'Customer Admin | CustSearch AI',
    canActivate: [authGuard, roleGuard(tenantRoles), permissionGuard([PERMISSIONS.tenantDashboardView])],
    loadComponent: () => import('./features/customer-admin/customer-dashboard').then(m => m.CustomerDashboard),
  },
  {
    path: 'platform-admin',
    redirectTo: 'admin/dashboard',
    pathMatch: 'full',
  },
  {
    path: 'admin/dashboard',
    title: 'Platform Admin | CustSearch AI',
    canActivate: [authGuard, roleGuard(platformRoles), permissionGuard([PERMISSIONS.tenantsOperationalSummary])],
    loadComponent: () => import('./features/platform-admin/platform-dashboard').then(m => m.PlatformDashboard),
  },
  {
    path: 'admin/tenants',
    canActivate: [authGuard, roleGuard(platformRoles), permissionGuard([PERMISSIONS.tenantsView])],
    children: [
      {
        path: '',
        title: 'Tenants | CustSearch AI',
        loadComponent: () => import('./features/platform-tenants/tenant-list-page').then(m => m.TenantListPage),
      },
      {
        path: 'new',
        title: 'Create tenant | CustSearch AI',
        canActivate: [permissionGuard([PERMISSIONS.tenantsCreate])],
        loadComponent: () => import('./features/platform-tenants/tenant-editor-page').then(m => m.TenantEditorPage),
      },
      {
        path: ':tenantId/edit',
        title: 'Edit tenant | CustSearch AI',
        canActivate: [permissionGuard([PERMISSIONS.tenantsEdit])],
        loadComponent: () => import('./features/platform-tenants/tenant-editor-page').then(m => m.TenantEditorPage),
      },
      {
        path: ':tenantId',
        title: 'Tenant details | CustSearch AI',
        loadComponent: () => import('./features/platform-tenants/tenant-detail-page').then(m => m.TenantDetailPage),
      },
    ],
  },
  {
    path: 'admin/subscription-plans', title: 'Subscription plans | CustSearch AI',
    canActivate: [authGuard, roleGuard(platformRoles), permissionGuard([PERMISSIONS.subscriptionPlansView])],
    loadComponent: () => import('./features/platform-tenants/subscription-plans-page').then(m => m.SubscriptionPlansPage),
  },
  { path: '**', redirectTo: 'login' },
];
