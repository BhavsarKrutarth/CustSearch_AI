/** Defines platform-owned tenant lifecycle values shown by the management workspace. */
export type TenantStatus = 'Active' | 'Suspended' | 'Inactive';

export interface PageResult<T> { items: T[]; page: number; pageSize: number; totalCount: number }
export interface TenantPlanSummary { id: number; planCode: string; planName: string }

/** Mirrors the safe tenant-directory projection returned by the platform API. */
export interface PlatformTenantListItem {
  id: number; tenantCode: string; legalName: string; displayName: string;
  primaryContactName: string; primaryEmail: string; primaryMobile: string | null;
  plan: TenantPlanSummary | null; storeCount: number; userCount: number; cameraCount: number;
  shopperCustomerCount: number; status: TenantStatus; subscriptionStatus: string;
  lastActivityUtc: string | null; version: string;
}

/** Mirrors the complete safe profile returned for one platform tenant. */
export interface PlatformTenantDetail {
  id: number; tenantCode: string; legalName: string; displayName: string; timeZone: string;
  primaryContactName: string; primaryEmail: string; primaryMobile: string | null;
  countryCode: string; currencyCode: string; status: TenantStatus; subscriptionStatus: string;
  plan: TenantPlanSummary | null; trialStartsUtc: string | null; trialEndsUtc: string | null;
  subscriptionStartsUtc: string | null; subscriptionEndsUtc: string | null;
  maxStores: number; maxUsers: number; maxCameras: number; suspensionReason: string | null;
  createdUtc: string; updatedUtc: string; version: string;
}

export interface PlatformTenantSummary {
  tenantId: number; tenantCode: string; status: string; subscriptionStatus: string; planName: string | null;
  stores: number; users: number; cameras: number; maxStores: number; maxUsers: number; maxCameras: number;
  monthlyRecognitions: number; monthlyApiCalls: number; usageCapturedUtc: string | null;
}

export interface TenantUsageItem {
  periodStartUtc: string; periodEndUtc: string; storeCount: number; userCount: number; cameraCount: number;
  recognitionCount: number; apiCallCount: number; capturedUtc: string;
}

export interface TenantAuditItem {
  id: number; tenantId: number | null; userId: number | null; actorType: string; action: string;
  entityType: string; entityId: string | null; beforeJson: string | null; afterJson: string | null;
  ipAddress: string | null; correlationId: string; createdUtc: string;
}

export interface SubscriptionPlanOption {
  id: number; planCode: string; planName: string; monthlyPrice: number; annualPrice: number | null;
  maxStores: number; maxUsers: number; maxCameras: number; maxMonthlyRecognitions: number | null;
  maxMonthlyApiCalls: number | null; isActive: boolean; version: string;
}

export interface TenantListQuery { page: number; pageSize: number; search?: string; status?: TenantStatus; planId?: number }
export interface CreateTenantRequest {
  legalName: string; displayName: string; timeZone: string;
  primaryContactName: string; primaryEmail: string; primaryMobile: string | null;
  countryCode: string; currencyCode: string; planId: number | null;
  maxStores: number | null; maxUsers: number | null; maxCameras: number | null;
  auditReason: string | null;
}
export interface UpdateTenantRequest {
  legalName: string; displayName: string; timeZone: string; primaryContactName: string;
  primaryEmail: string; primaryMobile: string | null; countryCode: string; currencyCode: string;
  expectedVersion: string;
}
export interface AssignTenantSubscriptionRequest {
  subscriptionPlanId: number; billingCycle: string; status: string; startsUtc: string; endsUtc: string | null;
  autoRenew: boolean; maxStores: number | null; maxUsers: number | null; maxCameras: number | null;
  maxMonthlyRecognitions: number | null; maxMonthlyApiCalls: number | null;
  expectedVersion: string; auditReason: string;
}
export interface PlatformDashboardSummary { totalTenants:number; activeTenants:number; trialTenants:number; suspendedTenants:number; inactiveTenants:number; monthlyRecurringRevenue:number; totalTenantUsers:number; totalCameras:number }
export interface SaveSubscriptionPlanRequest { planCode:string; planName:string; monthlyPrice:number; annualPrice:number|null; maxStores:number; maxUsers:number; maxCameras:number; maxMonthlyRecognitions:number|null; maxMonthlyApiCalls:number|null; isActive:boolean; expectedVersion:string|null }
