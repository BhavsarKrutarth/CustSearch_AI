import { PERMISSIONS } from '../auth/permission-catalog';

export interface AdminNavigationItem { label:string; route:string|null; permission:string; }

export const ADMIN_NAVIGATION:Record<'customer'|'platform',readonly AdminNavigationItem[]>={
  customer:[
    {label:'Dashboard',route:'/customer-admin',permission:PERMISSIONS.tenantDashboardView},
    {label:'Customers',route:'/customer-admin/customers',permission:PERMISSIONS.customersView},
    {label:'Households',route:'/customer-admin/households',permission:PERMISSIONS.householdsView},
    {label:'Visits',route:'/customer-admin/visits',permission:PERMISSIONS.visitsView},
    {label:'Visit Party / Co-Visit',route:'/customer-admin/visit-parties',permission:PERMISSIONS.visitPartiesView},
    {label:'Products',route:'/customer-admin/products',permission:PERMISSIONS.productsView},
    {label:'Categories',route:'/customer-admin/store-categories',permission:PERMISSIONS.storeCategoriesView},
    {label:'Voice commands',route:'/customer-admin/voice-commands',permission:PERMISSIONS.voiceCommandsView},
    {label:'Voice audit',route:'/customer-admin/voice-command-audit',permission:PERMISSIONS.voiceCommandsAudit},
    {label:'Retail invoices',route:'/customer-admin/retail/invoices',permission:PERMISSIONS.retailInvoicesView},
    {label:'Retail reports',route:'/customer-admin/retail/reports',permission:PERMISSIONS.retailReportsView},
    {label:'Alerts',route:'/customer-admin/alerts',permission:PERMISSIONS.alertsView},
    {label:'Retail security',route:'/admin/security/dashboard',permission:PERMISSIONS.securityIncidentsView},
    {label:'Integrations',route:'/customer-admin/integrations',permission:PERMISSIONS.integrationsView},
    {label:'Live monitoring',route:'/customer-admin/live-monitoring',permission:PERMISSIONS.camerasPreview},
    {label:'Camera operations',route:'/customer-admin/cameras',permission:PERMISSIONS.camerasView},
    {label:'Recognition review',route:'/customer-admin/recognition',permission:PERMISSIONS.recognitionView},
    {label:'Reports & exports',route:'/customer-admin/reports',permission:PERMISSIONS.reportsView},
  ],
  platform:[
    {label:'Dashboard',route:'/admin/dashboard',permission:PERMISSIONS.tenantsOperationalSummary},
    {label:'Tenants',route:'/admin/tenants',permission:PERMISSIONS.tenantsView},
    {label:'Tenant Users',route:'/admin/tenant-users',permission:PERMISSIONS.tenantsView},
    {label:'Stores',route:'/admin/stores',permission:PERMISSIONS.tenantsView},
    {label:'Platform Billing',route:'/platform-admin/billing/plans',permission:PERMISSIONS.platformBillingPlansView},
    {label:'Subscriptions',route:'/admin/subscription-plans',permission:PERMISSIONS.subscriptionPlansView},
    {label:'Reports',route:'/admin/reports',permission:PERMISSIONS.platformReportsView},
    {label:'Operations',route:'/admin/operations',permission:PERMISSIONS.platformOperationsView},
  ],
};
