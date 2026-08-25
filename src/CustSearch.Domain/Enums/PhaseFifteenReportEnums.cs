namespace CustSearch.Domain.Enums;

public enum ReportScope:byte{Platform=1,Tenant=2}
public enum ReportType:byte{OperationalSummary=1,Customers=2,Households=3,Visits=4,RetailBilling=5,Preferences=6,Alerts=7,Integrations=8,Cameras=9,StaffOperations=10,PlatformTenants=20}
public enum ExportFormat:byte{Csv=1,Excel=2,Pdf=3}
public enum ExportJobStatus:byte{Queued=1,Processing=2,Completed=3,Failed=4,Expired=5}
