import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { TenantApiClient } from '../../core/api/tenant-api.client';

export type CameraStatus=1|2|3|4;export type CameraDirection=1|2|3|4;export type CameraZoneType=1|2|3|4|5|6|7;export type TrackingSubjectKind=1|2|3;
export interface CameraView{id:number;storeId:number;cameraCode:string;name:string;hasRtspConfiguration:boolean;rtspConfigurationHint:string|null;status:CameraStatus;location:string|null;direction:CameraDirection;isActive:boolean;lastHeartbeatUtc:string|null;createdUtc:string;updatedUtc:string;}
export interface SaveCameraRequest{storeId:number;cameraCode:string;name:string;rtspConfigurationReference?:string|null;location?:string|null;direction:CameraDirection;isActive:boolean;}
export interface CameraZoneView{id:number;cameraId:number;zoneCode:string;name:string;zoneType:CameraZoneType;geometryJson:string;version:number;categoryId:number|null;effectiveUtc:string;supersededUtc:string|null;isActive:boolean;}
export interface SaveCameraZoneRequest{zoneCode:string;name:string;zoneType:CameraZoneType;geometryJson:string;categoryId?:number|null;}
export interface PersonTrackView{id:number;storeId:number;cameraId:number;personTrackId:string;startUtc:string;endUtc:string|null;confidence:number;trackingState:number;subjectKind:TrackingSubjectKind;customerId:number|null;staffProfileId:number|null;updatedUtc:string;}
export interface CctvCapabilities{demoMode:boolean;environment:string;identityRecognition:false;databaseAccessFromPython:false;}

/** Camera client deliberately has no TenantId input and never receives the full RTSP reference. */
@Injectable({providedIn:'root'})
export class CamerasApiService{
  private readonly api=inject(TenantApiClient);
  cameras(storeId?:number):Observable<CameraView[]>{return this.api.get<CameraView[]>(`cameras${storeId?`?storeId=${storeId}`:''}`);}
  create(request:SaveCameraRequest):Observable<CameraView>{return this.api.post<CameraView>('cameras',request);}
  update(id:number,request:SaveCameraRequest):Observable<CameraView>{return this.api.put<CameraView>(`cameras/${id}`,request);}
  zones(cameraId:number):Observable<CameraZoneView[]>{return this.api.get<CameraZoneView[]>(`cameras/${cameraId}/zones`);}
  addZone(cameraId:number,request:SaveCameraZoneRequest):Observable<CameraZoneView>{return this.api.post<CameraZoneView>(`cameras/${cameraId}/zones`,request);}
  tracks(storeId?:number,afterId?:number,take=100):Observable<PersonTrackView[]>{const query=new URLSearchParams();if(storeId)query.set('storeId',String(storeId));if(afterId)query.set('afterId',String(afterId));query.set('take',String(take));return this.api.get<PersonTrackView[]>(`cameras/tracks?${query}`);}
  associate(trackId:number,subjectKind:2|3,subjectId:number):Observable<PersonTrackView>{return this.api.post<PersonTrackView>(`cameras/tracks/${trackId}/associate`,{subjectKind,subjectId});}
  capabilities():Observable<CctvCapabilities>{return this.api.get<CctvCapabilities>('cameras/capabilities');}
}
