import { Routes } from '@angular/router';
import { authGuard, permissionGuard, roleGuard } from './core/auth/auth.guards';
import { PERMISSIONS } from './core/auth/permission-catalog';

const platformRoles=['PlatformSuperAdmin','PlatformOperationsAdmin','PlatformBillingAdmin','PlatformSupportAdmin','PlatformAuditor'];
const tenantRoles=['TenantAdmin','TenantOwner','ShopOwner','StoreAdmin','StoreManager','Manager','SalesStaff','CRMStaff','BillingStaff','CameraOperator','IntegrationAdmin','Auditor'];
const phase5Page=()=>import('./features/customer-admin/phase-five-management-page').then(m=>m.PhaseFiveManagementPage);
const platformBillingPage=()=>import('./features/platform-billing/platform-billing-page').then(m=>m.PlatformBillingPage);
const tenantBillingPage=()=>import('./features/platform-billing/tenant-billing-page').then(m=>m.TenantBillingPage);
const voicePreferencesPage=()=>import('./features/preferences/voice-preferences-page').then(m=>m.VoicePreferencesPage);

export const routes:Routes=[
  {path:'login',title:'Sign in | CustSearch AI',loadComponent:()=>import('./features/auth/login-page').then(m=>m.LoginPage)},
  {path:'',pathMatch:'full',redirectTo:'login'},
  {path:'access-denied',title:'Access denied | CustSearch AI',loadComponent:()=>import('./features/auth/access-denied-page').then(m=>m.AccessDeniedPage)},
  {path:'customer-admin',pathMatch:'full',redirectTo:'customer-admin/dashboard'},
  {path:'customer-admin/dashboard',title:'Customer Admin | CustSearch AI',canActivate:[authGuard,roleGuard(tenantRoles),permissionGuard([PERMISSIONS.tenantDashboardView])],loadComponent:()=>import('./features/customer-admin/phase-five-dashboard-page').then(m=>m.PhaseFiveDashboardPage)},
  {path:'customer-admin/stores',title:'Stores | CustSearch AI',data:{mode:'stores',title:'Stores'},canActivate:[authGuard,roleGuard(tenantRoles),permissionGuard([PERMISSIONS.tenantStoresView])],loadComponent:phase5Page},
  {path:'customer-admin/users',title:'Users | CustSearch AI',data:{mode:'users',title:'Tenant users'},canActivate:[authGuard,roleGuard(tenantRoles),permissionGuard([PERMISSIONS.tenantUsersView])],loadComponent:phase5Page},
  {path:'customer-admin/staff',title:'Staff | CustSearch AI',data:{mode:'staff',title:'Staff'},canActivate:[authGuard,roleGuard(tenantRoles),permissionGuard([PERMISSIONS.staffView])],loadComponent:phase5Page},
  {path:'customer-admin/staff/:id',title:'Staff detail | CustSearch AI',canActivate:[authGuard,roleGuard(tenantRoles),permissionGuard([PERMISSIONS.staffView])],loadComponent:()=>import('./features/customer-admin/phase-five-staff-detail-page').then(m=>m.PhaseFiveStaffDetailPage)},
  {path:'customer-admin/store-categories',title:'Categories | CustSearch AI',data:{mode:'categories',title:'Store categories'},canActivate:[authGuard,roleGuard(tenantRoles),permissionGuard([PERMISSIONS.storeCategoriesView])],loadComponent:phase5Page},
  {path:'customer-admin/categories',pathMatch:'full',redirectTo:'customer-admin/store-categories'},
  {path:'customer-admin/voice-commands',title:'Voice commands | CustSearch AI',canActivate:[authGuard,roleGuard(tenantRoles),permissionGuard([PERMISSIONS.voiceCommandsView])],loadComponent:voicePreferencesPage},
  {path:'customer-admin/voice-command-audit',title:'Voice command audit | CustSearch AI',canActivate:[authGuard,roleGuard(tenantRoles),permissionGuard([PERMISSIONS.voiceCommandsAudit])],loadComponent:voicePreferencesPage},
  {path:'customer-admin/voice-settings',pathMatch:'full',redirectTo:'customer-admin/voice-commands'},
  {path:'customer-admin/customers',title:'Customers | CustSearch AI',canActivate:[authGuard,roleGuard(tenantRoles),permissionGuard([PERMISSIONS.customersView])],loadComponent:()=>import('./features/customers/customer-list-page').then(m=>m.CustomerListPage)},
  {path:'customer-admin/customers/:id',title:'Customer profile | CustSearch AI',canActivate:[authGuard,roleGuard(tenantRoles),permissionGuard([PERMISSIONS.customersView])],loadComponent:()=>import('./features/customers/customer-detail-page').then(m=>m.CustomerDetailPage)},
  {path:'customer-admin/visitors',title:'Anonymous visitors | CustSearch AI',canActivate:[authGuard,roleGuard(tenantRoles),permissionGuard([PERMISSIONS.visitorsView])],loadComponent:()=>import('./features/visitors/visitor-list-page').then(m=>m.VisitorListPage)},
  {path:'customer-admin/households',title:'Households | CustSearch AI',canActivate:[authGuard,roleGuard(tenantRoles),permissionGuard([PERMISSIONS.householdsView])],loadComponent:()=>import('./features/households/household-list-page').then(m=>m.HouseholdListPage)},
  {path:'customer-admin/households/:id',title:'Household detail | CustSearch AI',canActivate:[authGuard,roleGuard(tenantRoles),permissionGuard([PERMISSIONS.householdsView])],loadComponent:()=>import('./features/households/household-detail-page').then(m=>m.HouseholdDetailPage)},
  {path:'customer-admin/visits',title:'Customer visits | CustSearch AI',canActivate:[authGuard,roleGuard(tenantRoles),permissionGuard([PERMISSIONS.visitsView])],loadComponent:()=>import('./features/visits/visit-list-page').then(m=>m.VisitListPage)},
  {path:'customer-admin/visit-parties',title:'Visit Party / Co-Visit | CustSearch AI',canActivate:[authGuard,roleGuard(tenantRoles),permissionGuard([PERMISSIONS.visitPartiesView])],loadComponent:()=>import('./features/visits/visit-party-list-page').then(m=>m.VisitPartyListPage)},

  // Phase 8 Retail Billing routes — shop-customer purchases.
  {path:'customer-admin/products',title:'Products | CustSearch AI',canActivate:[authGuard,roleGuard(tenantRoles),permissionGuard([PERMISSIONS.productsView])],loadComponent:()=>import('./features/retail/product-list-page').then(m=>m.ProductListPage)},
  {path:'customer-admin/retail/invoices',title:'Retail invoices | CustSearch AI',canActivate:[authGuard,roleGuard(tenantRoles),permissionGuard([PERMISSIONS.retailInvoicesView])],loadComponent:()=>import('./features/retail/invoice-list-page').then(m=>m.InvoiceListPage)},
  {path:'customer-admin/retail/invoices/new',title:'Create retail invoice | CustSearch AI',canActivate:[authGuard,roleGuard(tenantRoles),permissionGuard([PERMISSIONS.retailInvoicesCreate])],loadComponent:()=>import('./features/retail/invoice-editor-page').then(m=>m.InvoiceEditorPage)},
  {path:'customer-admin/retail/invoices/:id',title:'Retail invoice | CustSearch AI',canActivate:[authGuard,roleGuard(tenantRoles),permissionGuard([PERMISSIONS.retailInvoicesView])],loadComponent:()=>import('./features/retail/invoice-detail-page').then(m=>m.InvoiceDetailPage)},
  {path:'customer-admin/retail/reports',title:'Retail reports | CustSearch AI',canActivate:[authGuard,roleGuard(tenantRoles),permissionGuard([PERMISSIONS.retailReportsView])],loadComponent:()=>import('./features/retail/retail-reports-page').then(m=>m.RetailReportsPage)},

  // Phase 11 alert center is REST-authoritative and supplements state with authenticated SignalR events.
  {path:'customer-admin/alerts',title:'Notification center | CustSearch AI',canActivate:[authGuard,roleGuard(tenantRoles),permissionGuard([PERMISSIONS.alertsView])],loadComponent:()=>import('./features/alerts/notification-center-page').then(m=>m.NotificationCenterPage)},

  {path:'customer-admin/integrations',title:'Integration Settings | CustSearch AI',canActivate:[authGuard,roleGuard(tenantRoles),permissionGuard([PERMISSIONS.integrationsView])],loadComponent:()=>import('./features/integrations/integration-settings-page').then(m=>m.IntegrationSettingsPage)},

  // Phase 9 Platform Billing tenant views — CustSearch subscription billing only.
  {path:'customer-admin/billing',title:'CustSearch billing | CustSearch AI',data:{mode:'summary'},canActivate:[authGuard,roleGuard(tenantRoles),permissionGuard([PERMISSIONS.tenantPlatformBillingSubscriptionsView])],loadComponent:tenantBillingPage},
  {path:'customer-admin/billing/subscription',title:'Subscription | CustSearch AI',data:{mode:'subscription'},canActivate:[authGuard,roleGuard(tenantRoles),permissionGuard([PERMISSIONS.tenantPlatformBillingSubscriptionsView])],loadComponent:tenantBillingPage},
  {path:'customer-admin/billing/invoices',title:'Platform invoice history | CustSearch AI',data:{mode:'invoices'},canActivate:[authGuard,roleGuard(tenantRoles),permissionGuard([PERMISSIONS.tenantPlatformBillingInvoicesView])],loadComponent:tenantBillingPage},

  {path:'platform-admin',redirectTo:'admin/dashboard',pathMatch:'full'},
  {path:'admin/dashboard',title:'Platform Admin | CustSearch AI',canActivate:[authGuard,roleGuard(platformRoles),permissionGuard([PERMISSIONS.tenantsOperationalSummary])],loadComponent:()=>import('./features/platform-admin/platform-dashboard').then(m=>m.PlatformDashboard)},
  {path:'admin/tenants',canActivate:[authGuard,roleGuard(platformRoles),permissionGuard([PERMISSIONS.tenantsView])],children:[
    {path:'',title:'Tenants | CustSearch AI',loadComponent:()=>import('./features/platform-tenants/tenant-list-page').then(m=>m.TenantListPage)},
    {path:'new',title:'Create tenant | CustSearch AI',canActivate:[permissionGuard([PERMISSIONS.tenantsCreate])],loadComponent:()=>import('./features/platform-tenants/tenant-editor-page').then(m=>m.TenantEditorPage)},
    {path:':tenantId/edit',title:'Edit tenant | CustSearch AI',canActivate:[permissionGuard([PERMISSIONS.tenantsEdit])],loadComponent:()=>import('./features/platform-tenants/tenant-editor-page').then(m=>m.TenantEditorPage)},
    {path:':tenantId',title:'Tenant details | CustSearch AI',loadComponent:()=>import('./features/platform-tenants/tenant-detail-page').then(m=>m.TenantDetailPage)},
  ]},
  {path:'admin/subscription-plans',title:'Subscription plans | CustSearch AI',canActivate:[authGuard,roleGuard(platformRoles),permissionGuard([PERMISSIONS.subscriptionPlansView])],loadComponent:()=>import('./features/platform-tenants/subscription-plans-page').then(m=>m.SubscriptionPlansPage)},

  // Exact Phase 9 platform-admin billing routes requested by the plan.
  {path:'platform-admin/billing/plans',title:'Platform billing plans | CustSearch AI',data:{mode:'plans'},canActivate:[authGuard,roleGuard(platformRoles),permissionGuard([PERMISSIONS.platformBillingPlansView])],loadComponent:platformBillingPage},
  {path:'platform-admin/billing/subscriptions',title:'Tenant subscriptions | CustSearch AI',data:{mode:'subscriptions'},canActivate:[authGuard,roleGuard(platformRoles),permissionGuard([PERMISSIONS.platformBillingSubscriptionsView])],loadComponent:platformBillingPage},
  {path:'platform-admin/billing/invoices',title:'Platform invoices | CustSearch AI',data:{mode:'invoices'},canActivate:[authGuard,roleGuard(platformRoles),permissionGuard([PERMISSIONS.platformBillingInvoicesView])],loadComponent:platformBillingPage},
  {path:'platform-admin/billing/payments',title:'Platform payments | CustSearch AI',data:{mode:'payments'},canActivate:[authGuard,roleGuard(platformRoles),permissionGuard([PERMISSIONS.platformBillingPaymentsView])],loadComponent:platformBillingPage},
  {path:'**',redirectTo:'login'},
];
