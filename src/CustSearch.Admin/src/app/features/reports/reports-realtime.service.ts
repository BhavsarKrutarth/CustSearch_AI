import { Injectable, inject, signal } from '@angular/core';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { AuthSessionService } from '../../core/auth/auth-session.service';

export interface ReportExportRealtimeEvent{eventId:number;jobId:number;tenantId:number|null;requestedByUserId:number;eventType:string;status:number;progressPercent:number;occurredUtc:string;}

/** Authenticated user-only report progress channel; REST job history remains the recovery source. */
@Injectable({providedIn:'root'})
export class ReportsRealtimeService{
  private readonly session=inject(AuthSessionService);private readonly subject=new Subject<ReportExportRealtimeEvent>();private connection:HubConnection|null=null;readonly connected=signal(false);readonly events$=this.subject.asObservable();
  async start():Promise<void>{if(this.connection||!this.session.isAuthenticated())return;const connection=new HubConnectionBuilder().withUrl('/hubs/reports',{accessTokenFactory:()=>this.session.accessToken()??''}).withAutomaticReconnect([0,2000,5000,10000]).configureLogging(LogLevel.Warning).build();this.connection=connection;connection.on('ReportExportEvent',(event:ReportExportRealtimeEvent)=>{if(Number.isSafeInteger(event.eventId)&&event.eventId>0)this.subject.next(event);});connection.onreconnecting(()=>this.connected.set(false));connection.onreconnected(()=>this.connected.set(true));connection.onclose(()=>{this.connected.set(false);this.connection=null;});try{await connection.start();this.connected.set(true);}catch{this.connected.set(false);this.connection=null;}}
  async stop():Promise<void>{const connection=this.connection;this.connection=null;this.connected.set(false);if(connection)await connection.stop();}
}

