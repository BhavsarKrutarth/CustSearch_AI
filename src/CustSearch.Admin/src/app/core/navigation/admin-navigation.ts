import { PERMISSIONS } from '../auth/permission-catalog';

/** Describes one visible shell destination and the permission required to display it. */
export interface AdminNavigationItem {
  label: string;
  route: string | null;
  permission: string;
}

/** Centralizes Platform and Customer navigation so route metadata and menu filtering stay aligned. */
export const ADMIN_NAVIGATION: Record<'customer' | 'platform', readonly AdminNavigationItem[]> = {
  customer: [
    { label: 'Dashboard', route: '/customer-admin', permission: PERMISSIONS.tenantDashboardView },
    { label: 'Customers', route: '/customer-admin/customers', permission: PERMISSIONS.customersView },
    { label: 'Households', route: '/customer-admin/households', permission: PERMISSIONS.householdsView },
    { label: 'Visits', route: '/customer-admin/visits', permission: PERMISSIONS.visitsView },
    { label: 'Visit Party / Co-Visit', route: '/customer-admin/visit-parties', permission: PERMISSIONS.visitPartiesView },
    { label: 'Invoices & Payments', route: null, permission: PERMISSIONS.invoicesView },
    { label: 'Reports', route: null, permission: PERMISSIONS.reportsView },
    { label: 'Monitoring', route: null, permission: PERMISSIONS.camerasView },
  ],
  platform: [
    { label: 'Dashboard', route: '/admin/dashboard', permission: PERMISSIONS.tenantsOperationalSummary },
    { label: 'Tenants', route: '/admin/tenants', permission: PERMISSIONS.tenantsView },
    { label: 'Tenant Users', route: null, permission: PERMISSIONS.tenantsView },
    { label: 'Stores', route: null, permission: PERMISSIONS.tenantsView },
    { label: 'Platform Billing', route: null, permission: PERMISSIONS.platformBillingView },
    { label: 'Subscriptions', route: '/admin/subscription-plans', permission: PERMISSIONS.subscriptionPlansView },
    { label: 'Reports', route: null, permission: PERMISSIONS.platformReportsView },
  ],
};