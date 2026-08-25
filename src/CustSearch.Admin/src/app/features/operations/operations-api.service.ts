import {HttpClient} from '@angular/common/http';
import {Injectable,inject} from '@angular/core';

export interface WorkerControl{workerType:string;isPaused:boolean;reason:string|null;updatedUtc:string;}
export interface QueueHealth{notificationBacklog:number;notificationDeadLetters:number;integrationBacklog:number;integrationDeadLetters:number;exportBacklog:number;exportFailures:number;oldestPendingUtc:string|null;}
export interface DependencyHealth{name:string;status:string;detail:string;}
export interface OperationalHealth{dependencies:DependencyHealth[];workers:WorkerControl[];queues:QueueHealth;}
export interface OperationalSetting{id:number;scope:number;tenantId:number|null;storeId:number|null;key:string;valueJson:string;updatedUtc:string;}
export interface SecretReference{id:number;scope:number;tenantId:number|null;storeId:number|null;key:string;maskedReference:string;updatedUtc:string;}
export interface RetentionPolicy{id:number;domain:number;tenantId:number|null;storeId:number|null;retentionDays:number;enabled:boolean;updatedUtc:string;}

@Injectable({providedIn:'root'})export class OperationsApiService{private readonly http=inject(HttpClient);private readonly root='/api/platform/operations';health(){return this.http.get<OperationalHealth>(`${this.root}/health`);}settings(){return this.http.get<OperationalSetting[]>(`${this.root}/settings`);}secrets(){return this.http.get<SecretReference[]>(`${this.root}/secret-references`);}retention(){return this.http.get<RetentionPolicy[]>(`${this.root}/retention`);}pause(workerType:string,reason:string){return this.http.post<WorkerControl>(`${this.root}/workers/${encodeURIComponent(workerType)}/pause`,{reason});}resume(workerType:string){return this.http.post<WorkerControl>(`${this.root}/workers/${encodeURIComponent(workerType)}/resume`,{});}retry(queue:string,id:number){return this.http.post<void>(`${this.root}/dead-letters/${encodeURIComponent(queue)}/${id}/retry`,{});}}

