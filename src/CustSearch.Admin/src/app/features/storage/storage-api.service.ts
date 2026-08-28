import {HttpClient} from '@angular/common/http';
import {Injectable,inject} from '@angular/core';

export interface StoragePolicy{tenantId:number;storageEnabled:boolean;storageQuotaBytes:number;defaultRetentionDays:number;motionSnapshotRetentionDays:number;motionClipRetentionDays:number;falsePositiveRetentionDays:number;unreviewedEvidenceRetentionDays:number;confirmedIncidentRetentionDays:number;warningPercent:number;criticalPercent:number;allowSnapshots:boolean;allowMotionClips:boolean;autoCleanupEnabled:boolean;quotaPressurePolicy:1;updatedUtc:string;}
export interface StorageUsage{tenantId:number;quotaBytes:number;usedBytes:number;availableBytes:number;snapshotBytes:number;motionClipBytes:number;securityEvidenceBytes:number;otherBytes:number;usagePercent:number;pressureLevel:string;lastCalculatedUtc:string;lastCleanupUtc:string|null;}
export interface StorageSummary{policy:StoragePolicy;usage:StorageUsage;}
export type SaveStoragePolicy=Omit<StoragePolicy,'tenantId'|'updatedUtc'>;
@Injectable({providedIn:'root'})export class StorageApiService{private readonly http=inject(HttpClient);tenant(){return this.http.get<StorageSummary>('/api/tenant/storage');}platform(tenantId:number){return this.http.get<StorageSummary>(`/api/platform/tenants/${tenantId}/storage`);}savePlatform(tenantId:number,policy:SaveStoragePolicy){return this.http.put<StorageSummary>(`/api/platform/tenants/${tenantId}/storage/policy`,policy);}}
