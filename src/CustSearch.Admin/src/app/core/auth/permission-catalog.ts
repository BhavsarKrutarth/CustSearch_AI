/** Keeps Angular permission names identical to the backend's stable authorization catalog. */
export const PERMISSIONS = {
  tenantsView: 'Tenants.View', tenantsCreate: 'Tenants.Create', tenantsEdit: 'Tenants.Edit', tenantsActivate: 'Tenants.Activate', tenantsSuspend: 'Tenants.Suspend', tenantsViewUsage: 'Tenants.ViewUsage', tenantsOperationalSummary: 'Tenants.ViewOperationalSummary',
  platformBillingView: 'PlatformBilling.View', subscriptionPlansView: 'SubscriptionPlans.View', subscriptionPlansManage: 'SubscriptionPlans.Manage', platformReportsView: 'PlatformReports.View', platformAuditView: 'PlatformAudit.View',
  tenantDashboardView: 'TenantDashboard.View',
  tenantUsersView: 'TenantUsers.View', tenantUsersCreate: 'TenantUsers.Create', tenantUsersEdit: 'TenantUsers.Edit', tenantUsersDeactivate: 'TenantUsers.Deactivate', tenantUsersAssignRoles: 'TenantUsers.AssignRoles',
  tenantStoresView: 'TenantStores.View', tenantStoresCreate: 'TenantStores.Create', tenantStoresEdit: 'TenantStores.Edit',
  tenantBillingView: 'TenantBilling.View', tenantReportsView: 'TenantReports.View', tenantAuditView: 'TenantAudit.View',
  staffView: 'Staff.View', staffManage: 'Staff.Manage', staffTrackingView: 'StaffTracking.View',
  storeCategoriesView: 'StoreCategories.View', storeCategoriesManage: 'StoreCategories.Manage',
  voiceCommandsUse: 'VoiceCommands.Use', voiceCommandsView: 'VoiceCommands.View', voiceCommandsConfigure: 'VoiceCommands.Configure', voiceCommandsAudit: 'VoiceCommands.Audit',
  customersView: 'Customers.View', householdsView: 'Households.View', invoicesView: 'Invoices.View', reportsView: 'Reports.View', camerasView: 'Cameras.View', alertsView: 'Alerts.View', integrationsView: 'Integrations.View',
} as const;
