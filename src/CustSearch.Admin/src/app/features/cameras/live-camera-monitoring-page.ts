import { ChangeDetectionStrategy, Component, DestroyRef, ElementRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subscription, forkJoin, switchMap, timer } from 'rxjs';
import { AdminShell } from '../../shared/admin-shell/admin-shell';
import { CameraPreviewSessionView, CameraQuotaView, CameraView, CamerasApiService, CctvCapabilities } from './cameras-api.service';

type CameraFilter='all'|'online'|'offline';
type TileStatus='idle'|'connecting'|'live'|'unavailable'|'stopped';
interface TileState{status:TileStatus;message:string;session:CameraPreviewSessionView|null;objectUrl:string|null;}
const EMPTY_TILE:TileState={status:'idle',message:'Waiting to connect',session:null,objectUrl:null};

@Component({selector:'app-live-camera-monitoring-page',imports:[AdminShell],changeDetection:ChangeDetectionStrategy.OnPush,template:`
<app-admin-shell pageTitle="Live Camera Monitoring" eyebrow="Tenant operations · Secure multi-camera view">
  <p class="boundary">Only server-authorized camera frames are shown. RTSP addresses and credentials never reach this browser.</p>
  @if(demoMode()){<p class="demo" role="status">DEMO MODE — deterministic camera frames; no physical RTSP camera is connected.</p>}
  @if(error()){<p class="error" role="alert">{{error()}}</p>}

  <section class="summary" aria-label="Monitoring summary">
    <div><b>{{quota()?.activeCameras??'—'}} / {{quota()?.maxCameras??'—'}}</b><span>Active camera quota</span></div>
    <div><b>{{monitoredCameras().length}}</b><span>Monitoring grid</span></div>
    <div><b>{{onlineCount()}}</b><span>Online</span></div>
    <div><b>{{offlineCount()}}</b><span>Offline</span></div>
    <div><b>{{liveCount()}}</b><span>Live streams</span></div>
  </section>

  <section class="controls" aria-label="Camera filters">
    <button type="button" [attr.aria-pressed]="filter()==='all'" (click)="setFilter('all')">All cameras</button>
    <button type="button" [attr.aria-pressed]="filter()==='online'" (click)="setFilter('online')">Online</button>
    <button type="button" [attr.aria-pressed]="filter()==='offline'" (click)="setFilter('offline')">Offline</button>
    <button type="button" class="secondary" (click)="refresh()">Refresh grid</button>
  </section>

  @if(!previewEnabled()){<p class="error">Live preview runtime is disabled on the API server. Camera health remains visible.</p>}
  <section class="camera-grid" [class.two]="visibleCameras().length===2" aria-live="polite">
    @for(camera of visibleCameras();track camera.id){
      <article #tile class="camera-tile" [class.offline]="camera.status!==2">
        <header><div><b>{{camera.cameraCode}}</b><span>{{camera.name}}</span></div><span class="health" [class.online]="camera.status===2">{{health(camera.status)}}</span></header>
        <div class="viewport">
          @if(state(camera.id).objectUrl;as frame){<img [src]="frame" [alt]="camera.name+' live camera frame'">}
          @else{<div class="placeholder"><b>{{state(camera.id).status==='connecting'?'Connecting…':'Preview unavailable'}}</b><span>{{state(camera.id).message}}</span></div>}
        </div>
        <footer><span [class.live]="state(camera.id).status==='live'">{{state(camera.id).status==='live'?'LIVE':state(camera.id).status}}</span><small>{{camera.location||'Location not set'}} · heartbeat {{camera.lastHeartbeatUtc||'never'}}</small><div><button type="button" class="secondary" (click)="start(camera)">Retry</button><button type="button" class="secondary" (click)="stop(camera.id)">Stop</button><button type="button" (click)="fullscreen(tile)">Full screen</button></div></footer>
      </article>
    }
    @if(visibleCameras().length===0){<p class="empty">No active cameras match this filter.</p>}
  </section>
</app-admin-shell>`,styles:[`:host{display:block}.boundary{border-left:4px solid var(--color-accent);padding:.7rem 1rem}.demo{background:#5b3b00;border:2px solid #ffb020;border-radius:.5rem;color:#fff0c2;font-weight:800;padding:.8rem}.error{color:var(--color-danger)}.summary{display:grid;grid-template-columns:repeat(5,minmax(0,1fr));gap:.75rem;margin:1rem 0}.summary div,.camera-tile{background:var(--color-surface);border:1px solid var(--color-border);border-radius:var(--radius-sm)}.summary div{display:grid;padding:1rem}.summary span,small{color:var(--color-muted);font-size:.78rem}.controls{display:flex;flex-wrap:wrap;gap:.6rem;margin-bottom:1rem}.controls button,.camera-tile button{background:var(--color-accent);border:0;border-radius:var(--radius-sm);color:var(--color-on-accent);font-weight:700;padding:.6rem .85rem}.controls button[aria-pressed=true]{outline:3px solid color-mix(in srgb,var(--color-accent),white 35%)}.secondary{background:var(--color-nav-active)!important;color:var(--color-text)!important}.camera-grid{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:1rem}.camera-grid.two{grid-template-columns:repeat(2,minmax(0,1fr));max-width:75rem;margin-inline:auto}.camera-tile{display:grid;grid-template-rows:auto minmax(12rem,1fr) auto;min-width:0;overflow:hidden}.camera-tile header,.camera-tile footer{padding:.75rem}.camera-tile header{align-items:start;display:flex;justify-content:space-between}.camera-tile header div{display:grid}.health{background:var(--color-nav-active);border-radius:999px;padding:.25rem .55rem}.health.online,.live{color:#39d98a}.viewport{align-items:center;aspect-ratio:16/9;background:#050708;display:flex;justify-content:center;overflow:hidden}.viewport img{height:100%;object-fit:contain;width:100%}.placeholder{color:#d8e0e8;display:grid;gap:.4rem;padding:1rem;text-align:center}.placeholder span{color:#9ba9b8;font-size:.8rem}.camera-tile footer{display:grid;gap:.55rem}.camera-tile footer>div{display:flex;flex-wrap:wrap;gap:.4rem}.camera-tile.offline{border-color:#8a5b20}.empty{grid-column:1/-1;padding:3rem;text-align:center}@media(max-width:1100px){.summary{grid-template-columns:repeat(2,minmax(0,1fr))}.camera-grid{grid-template-columns:repeat(2,minmax(0,1fr))}}@media(max-width:700px){.summary,.camera-grid,.camera-grid.two{grid-template-columns:1fr}}`],})
export class LiveCameraMonitoringPage implements OnInit{
  private readonly api=inject(CamerasApiService);private readonly destroyRef=inject(DestroyRef);private readonly frameSubscriptions=new Map<number,Subscription>();
  protected readonly cameras=signal<CameraView[]>([]);protected readonly quota=signal<CameraQuotaView|null>(null);protected readonly capabilities=signal<CctvCapabilities|null>(null);protected readonly tiles=signal<Record<number,TileState>>({});protected readonly filter=signal<CameraFilter>('all');protected readonly error=signal('');
  protected readonly monitoredCameras=computed(()=>this.cameras().filter(camera=>camera.isActive).slice(0,5));protected readonly visibleCameras=computed(()=>this.monitoredCameras().filter(camera=>this.filter()==='all'||(this.filter()==='online'?camera.status===2:camera.status!==2)));protected readonly onlineCount=computed(()=>this.monitoredCameras().filter(camera=>camera.status===2).length);protected readonly offlineCount=computed(()=>this.monitoredCameras().length-this.onlineCount());protected readonly liveCount=computed(()=>Object.values(this.tiles()).filter(tile=>tile.status==='live').length);protected readonly demoMode=computed(()=>this.capabilities()?.demoMode===true);protected readonly previewEnabled=computed(()=>this.capabilities()?.previewEnabled===true);

  ngOnInit():void{this.destroyRef.onDestroy(()=>this.stopAll());this.load();}
  protected refresh():void{this.stopAll();this.load();}
  protected setFilter(value:CameraFilter):void{this.filter.set(value);this.reconcile();}
  protected state(cameraId:number):TileState{return this.tiles()[cameraId]??EMPTY_TILE;}
  protected health(status:number):string{return({1:'Offline',2:'Online',3:'Degraded',4:'Maintenance'} as Record<number,string>)[status]??'Unknown';}
  protected start(camera:CameraView):void{if(!this.previewEnabled()||this.state(camera.id).session||this.frameSubscriptions.has(camera.id))return;this.patch(camera.id,{status:'connecting',message:'Requesting an authorized session'});this.api.startPreview(camera.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({next:session=>{if(!this.visibleCameras().some(item=>item.id===camera.id)){this.api.endPreview(camera.id,session.sessionId).subscribe({error:()=>undefined});return;}this.patch(camera.id,{session,status:'connecting',message:'Waiting for the first frame'});const frames=timer(0,session.refreshMilliseconds).pipe(switchMap(()=>this.api.previewFrame(camera.id,session.sessionId)),takeUntilDestroyed(this.destroyRef)).subscribe({next:blob=>this.showFrame(camera.id,blob),error:()=>this.release(camera.id,true,'Camera frame unavailable. Retry to reconnect.')});this.frameSubscriptions.set(camera.id,frames);},error:failure=>this.patch(camera.id,{status:'unavailable',message:failure?.error?.message||'Preview is not assigned or the camera is unavailable.'})});}
  protected stop(cameraId:number):void{this.release(cameraId,true,'Stopped by operator.');}
  protected fullscreen(element:HTMLElement|ElementRef<HTMLElement>):void{const target=element instanceof ElementRef?element.nativeElement:element;if(target.requestFullscreen)void target.requestFullscreen();}

  private load():void{this.error.set('');forkJoin({cameras:this.api.cameras(),quota:this.api.quota(),capabilities:this.api.capabilities()}).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({next:value=>{this.cameras.set(value.cameras);this.quota.set(value.quota);this.capabilities.set(value.capabilities);this.reconcile();},error:()=>this.error.set('Unable to load the authorized camera monitoring grid.')});}
  private reconcile():void{const visible=new Set(this.visibleCameras().map(camera=>camera.id));for(const cameraId of this.frameSubscriptions.keys())if(!visible.has(cameraId))this.release(cameraId,true,'Hidden by filter.');if(this.previewEnabled())for(const camera of this.visibleCameras())this.start(camera);}
  private showFrame(cameraId:number,blob:Blob):void{const previous=this.state(cameraId).objectUrl;const objectUrl=URL.createObjectURL(blob);if(previous)URL.revokeObjectURL(previous);this.patch(cameraId,{objectUrl,status:'live',message:'Live'});}
  private release(cameraId:number,remote:boolean,message:string):void{this.frameSubscriptions.get(cameraId)?.unsubscribe();this.frameSubscriptions.delete(cameraId);const current=this.state(cameraId);if(remote&&current.session)this.api.endPreview(cameraId,current.session.sessionId).subscribe({error:()=>undefined});if(current.objectUrl)URL.revokeObjectURL(current.objectUrl);this.patch(cameraId,{session:null,objectUrl:null,status:'stopped',message});}
  private stopAll():void{const ids=new Set([...this.frameSubscriptions.keys(),...Object.keys(this.tiles()).map(Number)]);for(const cameraId of ids)this.release(cameraId,true,'Monitoring closed.');}
  private patch(cameraId:number,value:Partial<TileState>):void{this.tiles.update(items=>({...items,[cameraId]:{...(items[cameraId]??EMPTY_TILE),...value}}));}
}
