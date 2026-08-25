import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';

export interface SystemSetting {id:number;tenantId:number|null;storeId:number|null;settingKey:string;valueType:number;settingValue:string;description:string|null;updatedByUserId:number|null;createdUtc:string;updatedUtc:string;sourceScope:string;}
export interface AuditLogItem {id:number;tenantId:number|null;storeId:number|null;userId:number|null;actorType:string;action:string;entityType:string;entityId:string|null;ipAddress:string|null;correlationId:string;createdUtc:string;}
export interface AuditLogPage {items:AuditLogItem[];totalCount:number;pageNumber:number;pageSize:number;}
export interface SystemHealth {core:{database:{databaseName:string;serverName:string;productVersion:string;checkedUtc:string;status:string};workers:{instanceId:string;workerName:string;healthStatus:string;lastHeartbeatUtc:string;lastError:string|null}[];queues:{reportQueueDepth:number;webhookQueueDepth:number;notificationQueueDepth:number;reportEventBacklog:number};cameras:{totalCameras:number;onlineCameras:number;nonOnlineCameras:number}};apiStatus:string;redisStatus:string;redisBackplaneStatus:string;signalRStatus:string;activeWebSocketConnections:number;webSocketReconnects:number;pythonAiStatus:string;}

@Injectable({providedIn:'root'})
export class OperationsApiService{
  private readonly http=inject(HttpClient);
  settings(platform:boolean,storeId?:number){let params=new HttpParams().set('effective','true');if(!platform&&storeId)params=params.set('storeId',storeId);return this.http.get<SystemSetting[]>(`${this.base(platform)}/settings`,{params});}
  save(platform:boolean,item:SystemSetting,storeId?:number){let params=new HttpParams();if(!platform&&storeId)params=params.set('storeId',storeId);return this.http.put<SystemSetting>(`${this.base(platform)}/settings/${encodeURIComponent(item.settingKey)}`,{valueType:item.valueType,settingValue:item.settingValue,description:item.description},{params});}
  audit(platform:boolean,query:{storeId?:number;action?:string;entityType?:string;pageNumber:number;pageSize:number}){let params=new HttpParams().set('pageNumber',query.pageNumber).set('pageSize',query.pageSize);if(!platform&&query.storeId)params=params.set('storeId',query.storeId);if(query.action)params=params.set('action',query.action);if(query.entityType)params=params.set('entityType',query.entityType);return this.http.get<AuditLogPage>(`${this.base(platform)}/audit`,{params});}
  health(){return this.http.get<SystemHealth>('/api/platform/operations/health');}
  private base(platform:boolean){return platform?'/api/platform/operations':'/api/tenant/operations';}
}
