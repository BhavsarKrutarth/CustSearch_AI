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
    {label:'Reports & exports',route:'/customer-admin/reports',permission:PERMISSIONS.tenantReportsView},
    {label:'Alerts',route:'/customer-admin/alerts',permission:PERMISSIONS.alertsView},
    {label:'Integrations',route:'/customer-admin/integrations',permission:PERMISSIONS.integrationsView},
    {label:'Camera operations',route:'/customer-admin/cameras',permission:PERMISSIONS.camerasView},
    {label:'Recognition review',route:'/customer-admin/recognition',permission:PERMISSIONS.recognitionView},
    {label:'Settings',route:'/customer-admin/settings',permission:PERMISSIONS.settingsView},
    {label:'Audit logs',route:'/customer-admin/audit-logs',permission:PERMISSIONS.auditLogsView},
  ],
  platform:[
    {label:'Dashboard',route:'/admin/dashboard',permission:PERMISSIONS.tenantsOperationalSummary},
    {label:'Tenants',route:'/admin/tenants',permission:PERMISSIONS.tenantsView},
    {label:'Tenant Users',route:null,permission:PERMISSIONS.tenantsView},
    {label:'Stores',route:null,permission:PERMISSIONS.tenantsView},
    {label:'Platform Billing',route:null,permission:PERMISSIONS.platformBillingView},
    {label:'Subscriptions',route:'/admin/subscription-plans',permission:PERMISSIONS.subscriptionPlansView},
    {label:'Reports',route:'/admin/reports',permission:PERMISSIONS.platformReportsView},
    {label:'System Health',route:'/admin/system-health',permission:PERMISSIONS.systemHealthView},
    {label:'Audit Logs',route:'/admin/audit-logs',permission:PERMISSIONS.platformAuditView},
    {label:'Settings',route:'/admin/settings',permission:PERMISSIONS.platformSettingsView},
  ],
};
