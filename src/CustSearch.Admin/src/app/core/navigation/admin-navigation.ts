import { PERMISSIONS } from '../auth/permission-catalog';

export interface AdminNavigationItem { label:string; route:string|null; permission:string; icon:string; }
export interface AdminNavigationGroup { label:string; items:readonly AdminNavigationItem[]; }

export const ADMIN_NAVIGATION:Record<'customer'|'platform',readonly AdminNavigationItem[]>={
  customer:[
    {label:'Dashboard',route:'/customer-admin',permission:PERMISSIONS.tenantDashboardView,icon:'dashboard'},
    {label:'Stores',route:'/customer-admin/stores',permission:PERMISSIONS.tenantStoresView,icon:'store'},
    {label:'Staff',route:'/customer-admin/staff',permission:PERMISSIONS.staffView,icon:'staff'},
    {label:'Users',route:'/customer-admin/users',permission:PERMISSIONS.tenantUsersView,icon:'users'},
    {label:'Customers',route:'/customer-admin/customers',permission:PERMISSIONS.customersView,icon:'users'},
    {label:'Visitors',route:'/customer-admin/visitors',permission:PERMISSIONS.visitorsView,icon:'users'},
    {label:'Households',route:'/customer-admin/households',permission:PERMISSIONS.householdsView,icon:'household'},
    {label:'Visits',route:'/customer-admin/visits',permission:PERMISSIONS.visitsView,icon:'visits'},
    {label:'Visit Party / Co-Visit',route:'/customer-admin/visit-parties',permission:PERMISSIONS.visitPartiesView,icon:'party'},
    {label:'Products',route:'/customer-admin/products',permission:PERMISSIONS.productsView,icon:'report'},
    {label:'Categories',route:'/customer-admin/store-categories',permission:PERMISSIONS.storeCategoriesView,icon:'store'},
    {label:'Voice settings',route:'/customer-admin/voice-commands',permission:PERMISSIONS.voiceCommandsView,icon:'voice'},
    {label:'Voice audit',route:'/customer-admin/voice-command-audit',permission:PERMISSIONS.voiceCommandsAudit,icon:'operations'},
    {label:'Retail invoices',route:'/customer-admin/retail/invoices',permission:PERMISSIONS.retailInvoicesView,icon:'billing'},
    {label:'Retail reports',route:'/customer-admin/retail/reports',permission:PERMISSIONS.retailReportsView,icon:'report'},
    {label:'Alerts',route:'/customer-admin/alerts',permission:PERMISSIONS.alertsView,icon:'alert'},
    {label:'Retail security',route:'/admin/security/dashboard',permission:PERMISSIONS.securityIncidentsView,icon:'security'},
    {label:'Integrations',route:'/customer-admin/integrations',permission:PERMISSIONS.integrationsView,icon:'integration'},
    {label:'Live monitoring',route:'/customer-admin/live-monitoring',permission:PERMISSIONS.camerasPreview,icon:'live'},
    {label:'Camera operations',route:'/customer-admin/cameras',permission:PERMISSIONS.camerasView,icon:'camera'},
    {label:'Evidence storage',route:'/customer-admin/storage',permission:PERMISSIONS.storageViewUsage,icon:'billing'},
    {label:'Recognition review',route:'/customer-admin/recognition',permission:PERMISSIONS.recognitionView,icon:'security'},
    {label:'Reports & exports',route:'/customer-admin/reports',permission:PERMISSIONS.reportsView,icon:'report'},
    {label:'Platform billing',route:'/customer-admin/billing',permission:PERMISSIONS.tenantPlatformBillingSubscriptionsView,icon:'billing'},
  ],
  platform:[
    {label:'Dashboard',route:'/admin/dashboard',permission:PERMISSIONS.tenantsOperationalSummary,icon:'dashboard'},
    {label:'Tenants',route:'/admin/tenants',permission:PERMISSIONS.tenantsView,icon:'tenant'},
    {label:'Tenant Users',route:'/admin/tenant-users',permission:PERMISSIONS.tenantsView,icon:'users'},
    {label:'Stores',route:'/admin/stores',permission:PERMISSIONS.tenantsView,icon:'store'},
    {label:'Platform Billing',route:'/platform-admin/billing/plans',permission:PERMISSIONS.platformBillingPlansView,icon:'billing'},
    {label:'Subscriptions',route:'/admin/subscription-plans',permission:PERMISSIONS.subscriptionPlansView,icon:'report'},
    {label:'Reports',route:'/admin/reports',permission:PERMISSIONS.platformReportsView,icon:'report'},
    {label:'Operations',route:'/admin/operations',permission:PERMISSIONS.platformOperationsView,icon:'operations'},
  ],
};
