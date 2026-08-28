namespace CustSearch.Application.Authorization;

/// <summary>Lists stable permission names shared by token issuance, API policies and the admin UI.</summary>
public static class PermissionCatalog
{
    public static class Platform
    {
        public const string TenantsView="Tenants.View"; public const string TenantsCreate="Tenants.Create"; public const string TenantsEdit="Tenants.Edit"; public const string TenantsActivate="Tenants.Activate"; public const string TenantsSuspend="Tenants.Suspend"; public const string TenantsViewUsage="Tenants.ViewUsage"; public const string TenantsViewOperationalSummary="Tenants.ViewOperationalSummary"; public const string BillingView="PlatformBilling.View"; public const string BillingManage="PlatformBilling.Manage"; public const string SubscriptionPlansView="SubscriptionPlans.View"; public const string SubscriptionPlansManage="SubscriptionPlans.Manage"; public const string ReportsView="PlatformReports.View"; public const string ReportsExport="PlatformReports.Export"; public const string OperationsView="PlatformOperations.View"; public const string OperationsManage="PlatformOperations.Manage"; public const string AuditView="PlatformAudit.View"; public const string SupportAccessTenant="PlatformSupport.AccessTenant";
    }

    /// <summary>Phase 9 platform-scope permissions for CustSearch billing administrators.</summary>
    public static class PlatformBilling
    {
        public const string PlansView="PlatformBilling.Plans.View";
        public const string PlansManage="PlatformBilling.Plans.Manage";
        public const string SubscriptionsView="PlatformBilling.Subscriptions.View";
        public const string SubscriptionsManage="PlatformBilling.Subscriptions.Manage";
        public const string InvoicesView="PlatformBilling.Invoices.View";
        public const string PaymentsView="PlatformBilling.Payments.View";
    }

    /// <summary>Phase 9 tenant-scope read-only permissions. Names are globally distinct from platform grants.</summary>
    public static class TenantPlatformBilling
    {
        public const string SubscriptionsView="TenantPlatformBilling.Subscriptions.View";
        public const string InvoicesView="TenantPlatformBilling.Invoices.View";
        public const string PaymentsView="TenantPlatformBilling.Payments.View";
    }

    public static class Tenant
    {
        public const string DashboardView="TenantDashboard.View"; public const string UsersView="TenantUsers.View"; public const string UsersCreate="TenantUsers.Create"; public const string UsersEdit="TenantUsers.Edit"; public const string UsersDeactivate="TenantUsers.Deactivate"; public const string UsersAssignRoles="TenantUsers.AssignRoles"; public const string StoresView="TenantStores.View"; public const string StoresCreate="TenantStores.Create"; public const string StoresEdit="TenantStores.Edit"; public const string BillingView="TenantBilling.View"; public const string ReportsView="TenantReports.View"; public const string ReportsExport="TenantReports.Export"; public const string AuditView="TenantAudit.View";
    }

    public static class Operations
    {
        public const string CamerasManageZones="Cameras.ManageZones";
        public const string CamerasPreview="Cameras.Preview"; public const string CamerasTrackingView="Cameras.TrackingView";
        public const string CustomersView="Customers.View"; public const string CustomersCreate="Customers.Create"; public const string CustomersEdit="Customers.Edit"; public const string VisitorsView="Visitors.View"; public const string VisitorsConvert="Visitors.Convert";
        public const string HouseholdsView="Households.View"; public const string HouseholdsCreate="Households.Create"; public const string HouseholdsEdit="Households.Edit"; public const string HouseholdsManageMembers="Households.ManageMembers"; public const string VisitsView="Visits.View"; public const string VisitsEdit="Visits.Edit"; public const string VisitPartiesView="VisitParties.View";
        public const string InvoicesView="Invoices.View"; public const string InvoicesCreate="Invoices.Create"; public const string InvoicesEdit="Invoices.Edit"; public const string PaymentsView="Payments.View"; public const string PaymentsCreate="Payments.Create";
        public const string ProductsView="Products.View"; public const string ProductsCreate="Products.Create"; public const string ProductsEdit="Products.Edit"; public const string ProductsManageStores="Products.ManageStores";
        public const string RetailInvoicesView="RetailInvoices.View"; public const string RetailInvoicesCreate="RetailInvoices.Create"; public const string RetailInvoicesEdit="RetailInvoices.Edit"; public const string RetailInvoicesFinalize="RetailInvoices.Finalize"; public const string RetailInvoicesCancel="RetailInvoices.Cancel";
        public const string RetailPaymentsView="RetailPayments.View"; public const string RetailPaymentsCreate="RetailPayments.Create"; public const string RetailSpendAttributionView="RetailSpendAttribution.View"; public const string RetailSpendAttributionManage="RetailSpendAttribution.Manage"; public const string RetailReportsView="RetailReports.View";
        public const string CamerasView="Cameras.View"; public const string CamerasManage="Cameras.Manage"; public const string CamerasManageRules="Cameras.ManageRules"; public const string CamerasControl="Cameras.Control"; public const string RecognitionView="Recognition.View"; public const string RecognitionEnroll="Recognition.Enroll"; public const string RecognitionReview="Recognition.Review"; public const string RecognitionSettingsManage="Recognition.Settings.Manage"; public const string RecognitionConsentManage="Recognition.Consent.Manage"; public const string PreferencesView="Preferences.View"; public const string PreferencesManage="Preferences.Manage"; public const string AlertsView="Alerts.View"; public const string AlertsAcknowledge="Alerts.Acknowledge"; public const string AlertsConfigure="Alerts.Configure"; public const string ConsentsView="Consents.View"; public const string ConsentsManage="Consents.Manage"; public const string IntegrationsView="Integrations.View"; public const string IntegrationsManage="Integrations.Manage"; public const string WebhooksView="Webhooks.View"; public const string WebhooksManage="Webhooks.Manage"; public const string ReportsView="Reports.View"; public const string ReportsExport="Reports.Export"; public const string UsersView="Users.View"; public const string UsersManage="Users.Manage"; public const string StaffView="Staff.View"; public const string StaffManage="Staff.Manage"; public const string StaffTrackingView="StaffTracking.View"; public const string StaffPerformanceView="StaffPerformance.View"; public const string StaffPerformanceExport="StaffPerformance.Export"; public const string StaffCustomerInteractionsView="StaffCustomerInteractions.View"; public const string StoreCategoriesView="StoreCategories.View"; public const string StoreCategoriesManage="StoreCategories.Manage"; public const string VoiceCommandsUse="VoiceCommands.Use"; public const string VoiceCommandsView="VoiceCommands.View"; public const string VoiceCommandsConfigure="VoiceCommands.Configure"; public const string VoiceCommandsAudit="VoiceCommands.Audit"; public const string CustomerJourneysView="CustomerJourneys.View"; public const string DwellAnalyticsView="DwellAnalytics.View"; public const string RolesManage="Roles.Manage"; public const string SettingsView="Settings.View"; public const string SettingsManage="Settings.Manage"; public const string AuditLogsView="AuditLogs.View";
    }

    /// <summary>Phase 18 tenant/store-scoped retail security permissions.</summary>
    public static class Security
    {
        public const string IncidentsView="Security.Incidents.View";
        public const string IncidentsAcknowledge="Security.Incidents.Acknowledge";
        public const string IncidentsAssign="Security.Incidents.Assign";
        public const string IncidentsReview="Security.Incidents.Review";
        public const string IncidentsConfirmLoss="Security.Incidents.ConfirmLoss";
        public const string IncidentsResolve="Security.Incidents.Resolve";
        public const string EvidenceView="Security.Evidence.View";
        public const string EvidenceExport="Security.Evidence.Export";
        public const string SettingsView="Security.Settings.View";
        public const string SettingsManage="Security.Settings.Manage";
        public const string RulesView="Security.Rules.View";
        public const string RulesManage="Security.Rules.Manage";
        public const string ReportsView="Security.Reports.View";
    }

    public static readonly IReadOnlySet<string> All=typeof(PermissionCatalog).GetNestedTypes().Where(type=>type!=typeof(PermissionCatalog)).SelectMany(type=>type.GetFields(System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Static)).Where(field=>field.IsLiteral&&field.FieldType==typeof(string)).Select(field=>(string)field.GetRawConstantValue()!).ToHashSet(StringComparer.Ordinal);
}
