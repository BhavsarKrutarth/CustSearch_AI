import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { TenantApiClient } from '../../core/api/tenant-api.client';

export type SecurityStatus=1|2|3|4|5|6|7|8|9;
export interface SecurityIncidentSummary{id:number;incidentNumber:string;storeId:number;personTrackId:string|null;severity:number;riskScore:number;ruleVersion:number;status:SecurityStatus;estimatedLossAmount:number|null;currency:string;assignedUserId:number|null;resolutionCode:string|null;createdUtc:string;updatedUtc:string;}
export interface SecurityItem{id:number;productId:number|null;productCategoryId:number|null;description:string;quantity:number|null;unitValue:number|null;productConfidence:number|null;paymentMatchStatus:number;}
export interface SecurityPayment{id:number;invoiceId:number|null;transactionReference:string|null;matchType:number;matchScore:number;matchedUtc:string;notes:string|null;}
export interface SecurityEvidence{id:number;evidenceType:number;cameraId:number|null;capturedUtc:string;available:boolean;retentionUntilUtc:string;restricted:boolean;}
export interface SecurityEvidenceTicket{token:string;expiresUtc:string;}
export interface SecurityAction{id:number;actionType:string;fromStatus:SecurityStatus|null;toStatus:SecurityStatus|null;userId:number|null;actorType:string;reasonCode:string|null;notes:string|null;occurredUtc:string;correlationId:string;}
export interface SecurityIncidentDetail{incident:SecurityIncidentSummary;items:SecurityItem[];paymentCorrelations:SecurityPayment[];evidence:SecurityEvidence[];timeline:SecurityAction[];}
export interface SecuritySettings{storeId:number|null;securityEnabled:boolean;unpaidExitDetectionEnabled:boolean;realtimeAlertsEnabled:boolean;shadowMode:boolean;riskThreshold:number;highValueThreshold:number;checkoutCorrelationWindowMinutes:number;exitGracePeriodSeconds:number;evidenceBeforeSeconds:number;evidenceAfterSeconds:number;evidenceRetentionDays:number;notificationChannels:string[];escalationPolicy:string;ruleVersion:number;}
export interface SecurityRule{id:number;storeId:number|null;ruleCode:string;name:string;enabled:boolean;severity:number;configurationJson:string;version:number;createdByUserId:number|null;createdUtc:string;}
export interface SecurityReport{storeId:number|null;fromUtc:string;toUtc:string;candidateCount:number;alertedCount:number;confirmedLossCount:number;falsePositiveCount:number;resolvedCount:number;averageRisk:number;precision:number|null;falsePositiveRate:number|null;}

@Injectable({providedIn:'root'})export class SecurityApiService{
 private readonly api=inject(TenantApiClient);
 list():Observable<SecurityIncidentSummary[]>{return this.api.get('security/incidents');}
 detail(id:number):Observable<SecurityIncidentDetail>{return this.api.get(`security/incidents/${id}`);}
 transition(id:number,action:'acknowledge'|'review'|'confirm-loss'|'false-positive'|'resolve',reason:string,notes=''):Observable<SecurityIncidentDetail>{return this.api.post(`security/incidents/${id}/${action}`,{reason,notes});}
 evidenceTicket(incidentId:number,evidenceId:number):Observable<SecurityEvidenceTicket>{return this.api.post(`security/incidents/${incidentId}/evidence/${evidenceId}/view-ticket`);}
 evidenceUrl(incidentId:number,evidenceId:number,token:string):string{return `/api/tenant/security/incidents/${incidentId}/evidence/${evidenceId}/view?token=${encodeURIComponent(token)}`;}
 settings():Observable<SecuritySettings>{return this.api.get('security/settings');}
 saveSettings(value:SecuritySettings):Observable<SecuritySettings>{const command:Partial<SecuritySettings>={...value};delete command.storeId;delete command.ruleVersion;return this.api.put('security/settings',command);}
 rules():Observable<SecurityRule[]>{return this.api.get('security/rules');}
 report(fromUtc:string,toUtc:string):Observable<SecurityReport>{return this.api.get(`security/reports?fromUtc=${encodeURIComponent(fromUtc)}&toUtc=${encodeURIComponent(toUtc)}`);}
}
