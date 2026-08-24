import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { TenantApiClient } from '../../core/api/tenant-api.client';

export type AlertSeverity=1|2|3;
export type AlertStatus=1|2|3|4|5;
export interface AlertView{id:number;alertType:string;storeId:number|null;severity:AlertSeverity;title:string;message:string;entityType:string;entityId:string|null;createdUtc:string;acknowledgedUtc:string|null;acknowledgedByUserId:number|null;resolvedUtc:string|null;status:AlertStatus;correlationId:string;deduplicationKey:string;}
export interface AlertListView{items:AlertView[];unreadCount:number;lastEventId:number;}
export interface AlertRealtimeEventV1{eventId:number;eventType:'alert.created'|'alert.updated'|'alert.acknowledged'|'alert.resolved';contractVersion:1;occurredUtc:string;tenantId:number;storeId:number|null;correlationId:string;alert:AlertView;}
export interface AlertRecoveryView{requestedAfterEventId:number;nextCursor:number;events:AlertRealtimeEventV1[];}
export interface AlertHealthMetricsView{outboxBacklog:number;deliverySuccesses:number;deliveryFailures:number;retries:number;deadLetters:number;oldestPendingUtc:string|null;signalRConnections:number;reconnects:number;}

/** Typed Phase 11 REST client. No method accepts or serializes TenantId. */
@Injectable({providedIn:'root'})
export class AlertsApiService{
  private readonly api=inject(TenantApiClient);
  list(storeId?:number,status?:AlertStatus):Observable<AlertListView>{const query=new URLSearchParams();if(storeId)query.set('storeId',String(storeId));if(status)query.set('status',String(status));return this.api.get<AlertListView>(`alerts${query.size?`?${query}`:''}`);}
  get(alertId:number):Observable<AlertView>{return this.api.get<AlertView>(`alerts/${alertId}`);}
  recover(afterEventId:number,take=200):Observable<AlertRecoveryView>{return this.api.get<AlertRecoveryView>(`alerts/recovery?afterEventId=${afterEventId}&take=${take}`);}
  acknowledge(alertId:number):Observable<AlertView>{return this.api.post<AlertView>(`alerts/${alertId}/acknowledge`,{});}
  resolve(alertId:number):Observable<AlertView>{return this.api.post<AlertView>(`alerts/${alertId}/resolve`,{});}
  metrics():Observable<AlertHealthMetricsView>{return this.api.get<AlertHealthMetricsView>('alerts/metrics');}
}
