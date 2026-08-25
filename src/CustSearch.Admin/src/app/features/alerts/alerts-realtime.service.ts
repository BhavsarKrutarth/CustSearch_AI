import { Injectable, inject, signal } from '@angular/core';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { Subject, firstValueFrom } from 'rxjs';
import { AuthSessionService } from '../../core/auth/auth-session.service';
import { AlertRealtimeEventV1, AlertsApiService } from './alerts-api.service';

export type RealtimeConnectionState='Disconnected'|'Connecting'|'Connected'|'Reconnecting'|'Failed';

/** Keeps a bounded set of durable event IDs so live delivery and REST replay cannot duplicate UI work. */
export class AlertEventDeduplicator{
  private readonly seen=new Set<number>();private readonly order:number[]=[];private cursorValue=0;
  get cursor():number{return this.cursorValue;}
  accept(event:AlertRealtimeEventV1):boolean{if(!Number.isSafeInteger(event.eventId)||event.eventId<=0||event.contractVersion!==1||this.seen.has(event.eventId))return false;this.seen.add(event.eventId);this.order.push(event.eventId);this.cursorValue=Math.max(this.cursorValue,event.eventId);if(this.order.length>1000){const oldest=this.order.shift();if(oldest!==undefined)this.seen.delete(oldest);}return true;}
  reset():void{this.seen.clear();this.order.length=0;this.cursorValue=0;}
}

/** Owns one authenticated SignalR connection and always recovers missed authoritative events after reconnect. */
@Injectable({providedIn:'root'})
export class AlertsRealtimeService{
  private readonly session=inject(AuthSessionService);private readonly api=inject(AlertsApiService);private readonly dedupe=new AlertEventDeduplicator();private readonly eventSubject=new Subject<AlertRealtimeEventV1>();private connection:HubConnection|null=null;private stopping=false;
  readonly connectionState=signal<RealtimeConnectionState>('Disconnected');readonly events$=this.eventSubject.asObservable();
  async start():Promise<void>{if(this.connection||!this.session.isAuthenticated())return;this.stopping=false;this.connectionState.set('Connecting');const connection=new HubConnectionBuilder().withUrl('/hubs/alerts',{accessTokenFactory:()=>this.session.accessToken()??''}).withAutomaticReconnect([0,2000,5000,10000,30000]).configureLogging(LogLevel.Warning).build();this.connection=connection;connection.on('AlertEvent',(event:AlertRealtimeEventV1)=>this.emit(event));connection.onreconnecting(()=>this.connectionState.set('Reconnecting'));connection.onreconnected(async()=>{this.connectionState.set('Connected');await connection.invoke('ReportReconnect',this.dedupe.cursor);await this.recoverMissed();});connection.onclose(()=>{this.connectionState.set(this.stopping||!this.session.isAuthenticated()?'Disconnected':'Failed');this.connection=null;});try{await connection.start();this.connectionState.set('Connected');try{await this.recoverMissed();}catch{/* Live connection remains useful; the page also reloads authoritative REST state. */}}catch{this.connectionState.set('Failed');this.connection=null;}}
  async stop():Promise<void>{this.stopping=true;const connection=this.connection;this.connection=null;this.dedupe.reset();this.connectionState.set('Disconnected');if(connection)await connection.stop();}
  private async recoverMissed():Promise<void>{const recovery=await firstValueFrom(this.api.recover(this.dedupe.cursor));for(const event of recovery.events)this.emit(event);}
  private emit(event:AlertRealtimeEventV1):void{if(this.dedupe.accept(event))this.eventSubject.next(event);}
}
