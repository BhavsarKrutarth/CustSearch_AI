import { Routes } from '@angular/router';
import { authGuard, permissionGuard, roleGuard } from './core/auth/auth.guards';
import { PERMISSIONS } from './core/auth/permission-catalog';

const platformRoles = ['PlatformSuperAdmin', 'PlatformOperationsAdmin', 'PlatformBillingAdmin', 'PlatformSupportAdmin', 'PlatformAuditor'];
const tenantRoles = ['TenantAdmin','TenantOwner','ShopOwner','StoreAdmin','StoreManager','Manager','SalesStaff','CRMStaff','BillingStaff','CameraOperator','IntegrationAdmin','Auditor'];
const phase5Page = () => import('./features/customer-admin/phase-five-management-page').then(m => m.PhaseFiveManagementPage);

export const routes: Routes = [
  { path:'login', title:'Sign in | CustSearch AI', loadComponent:()=>import('./features/auth/login-page').then(m=>m.LoginPage) },
  { path:'', pathMatch:'full', redirectTo:'login' },
  { path:'access-denied', title:'Access denied | CustSearch AI', loadComponent:()=>import('./features/auth/access-denied-page').then(m=>m.AccessDeniedPage) },
  { path:'customer-admin', pathMatch:'full', redirectTo:'customer-admin/dashboard' },
  { path:'customer-admin/dashboard', title:'Customer Admin | CustSearch AI', canActivate:[authGuard,roleGuard(tenantRoles),permissionGuard([PERMISSIONS.tenantDashboardView])], loadComponent:()=>import('./features/customer-admin/phase-five-dashboard-page').then(m=>m.PhaseFiveDashboardPage) },
  { path:'customer-admin/stores', title:'Stores | CustSearch AI', data:{mode:'stores',title:'Stores'}, canActivate:[authGuard,roleGuard(tenantRoles),permissionGuard([PERMISSIONS.tenantStoresView])], loadComponent:phase5Page },
  { path:'customer-admin/users', title:'Users | CustSearch AI', data:{mode:'users',title:'Tenant users'}, canActivate:[authGuard,roleGuard(tenantRoles),permissionGuard([PERMISSIONS.tenantUsersView])], loadComponent:phase5Page },
  { path:'customer-admin/staff', title:'Staff | CustSearch AI', data:{mode:'staff',title:'Staff'}, canActivate:[authGuard,roleGuard(tenantRoles),permissionGuard([PERMISSIONS.staffView])], loadComponent:phase5Page },
  { path:'customer-admin/staff/:id', title:'Staff detail | CustSearch AI', canActivate:[authGuard,roleGuard(tenantRoles),permissionGuard([PERMISSIONS.staffView])], loadComponent:()=>import('./features/customer-admin/phase-five-staff-detail-page').then(m=>m.PhaseFiveStaffDetailPage) },
  { path:'customer-admin/store-categories', title:'Categories | CustSearch AI', data:{mode:'categories',title:'Store categories'}, canActivate:[authGuard,roleGuard(tenantRoles),permissionGuard([PERMISSIONS.storeCategoriesView])], loadComponent:phase5Page },
  { path:'customer-admin/categories', pathMatch:'full', redirectTo:'customer-admin/store-categories' },
  { path:'customer-admin/voice-commands', title:'Voice commands | CustSearch AI', data:{mode:'voice',title:'Dynamic voice settings'}, canActivate:[authGuard,roleGuard(tenantRoles),permissionGuard([PERMISSIONS.voiceCommandsView])], loadComponent:phase5Page },
  { path:'customer-admin/voice-settings', pathMatch:'full', redirectTo:'customer-admin/voice-commands' },
  { path:'platform-admin', redirectTo:'admin/dashboard', pathMatch:'full' },
  { path:'admin/dashboard', title:'Platform Admin | CustSearch AI', canActivate:[authGuard,roleGuard(platformRoles),permissionGuard([PERMISSIONS.tenantsOperationalSummary])], loadComponent:()=>import('./features/platform-admin/platform-dashboard').then(m=>m.PlatformDashboard) },
  { path:'admin/tenants', canActivate:[authGuard,roleGuard(platformRoles),permissionGuard([PERMISSIONS.tenantsView])], children:[
    { path:'', title:'Tenants | CustSearch AI', loadComponent:()=>import('./features/platform-tenants/tenant-list-page').then(m=>m.TenantListPage) },
    { path:'new', title:'Create tenant | CustSearch AI', canActivate:[permissionGuard([PERMISSIONS.tenantsCreate])], loadComponent:()=>import('./features/platform-tenants/tenant-editor-page').then(m=>m.TenantEditorPage) },
    { path:':tenantId/edit', title:'Edit tenant | CustSearch AI', canActivate:[permissionGuard([PERMISSIONS.tenantsEdit])], loadComponent:()=>import('./features/platform-tenants/tenant-editor-page').then(m=>m.TenantEditorPage) },
    { path:':tenantId', title:'Tenant details | CustSearch AI', loadComponent:()=>import('./features/platform-tenants/tenant-detail-page').then(m=>m.TenantDetailPage) },
  ]},
  { path:'admin/subscription-plans', title:'Subscription plans | CustSearch AI', canActivate:[authGuard,roleGuard(platformRoles),permissionGuard([PERMISSIONS.subscriptionPlansView])], loadComponent:()=>import('./features/platform-tenants/subscription-plans-page').then(m=>m.SubscriptionPlansPage) },
  { path:'**', redirectTo:'login' },
];
