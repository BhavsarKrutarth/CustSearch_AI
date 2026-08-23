import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { TenantApiClient } from '../../core/api/tenant-api.client';

export interface DashboardSummary { activeUsers:number; activeStores:number; activeStaff:number; activeCategories:number; openShifts:number; activePresenceSessions:number; }
export interface TenantUser { id:number; userName:string; email:string; displayName:string; isActive:boolean; roles:string[]; storeIds:number[]; createdUtc:string; lastLoginUtc?:string|null; }
export interface Store { id:number; storeCode:string; storeName:string; addressLine1:string; addressLine2?:string|null; landmark?:string|null; city:string; district?:string|null; stateOrProvince:string; postalCode:string; countryCode:string; latitude?:number|null; longitude?:number|null; geoFenceRadiusMeters?:number|null; externalPlaceId?:string|null; locationSource:number; isLocationVerified:boolean; timeZone:string; contactEmail?:string|null; contactMobile?:string|null; isActive:boolean; }
export interface Staff { id:number; userId:number; employeeCode:string; firstName:string; lastName:string; mobile?:string|null; isActive:boolean; storeIds:number[]; }
export interface Category { id:number; storeId?:number|null; categoryCode:string; name:string; parentCategoryId?:number|null; isActive:boolean; }
export interface VoiceSetting { storeId:number; triggerKeyword:string; responseMode:number; isEnabled:boolean; requireConfirmationForAmbiguousCategory:boolean; aliases:string[]; updatedUtc:string; }

/** Typed Phase 5 tenant API client. TenantId is intentionally absent from every browser request model. */
@Injectable({providedIn:'root'})
export class PhaseFiveApiService {
  private readonly api=inject(TenantApiClient);
  dashboard():Observable<DashboardSummary>{return this.api.get('dashboard/summary');}
  users():Observable<TenantUser[]>{return this.api.get('users');}
  user(id:number):Observable<TenantUser>{return this.api.get(`users/${id}`);}
  createUser(body:unknown):Observable<TenantUser>{return this.api.post('users',body);}
  updateUser(id:number,body:unknown):Observable<TenantUser>{return this.api.put(`users/${id}`,body);}
  setUserRoles(id:number,roles:string[]):Observable<TenantUser>{return this.api.put(`users/${id}/roles`,{roles});}
  setUserStores(id:number,storeIds:number[],primaryStoreId?:number|null):Observable<TenantUser>{return this.api.put(`users/${id}/stores`,{storeIds,primaryStoreId:primaryStoreId??null});}
  stores():Observable<Store[]>{return this.api.get('stores');}
  createStore(body:unknown):Observable<Store>{return this.api.post('stores',body);}
  updateStore(id:number,body:unknown):Observable<Store>{return this.api.put(`stores/${id}`,body);}
  verifyStore(id:number):Observable<Store>{return this.api.post(`stores/${id}/verify-location`);}
  setStoreActive(id:number,active:boolean):Observable<Store>{return this.api.post(`stores/${id}/${active?'activate':'deactivate'}`);}
  staff():Observable<Staff[]>{return this.api.get('staff');}
  staffById(id:number):Observable<Staff>{return this.api.get(`staff/${id}`);}
  createStaff(body:unknown):Observable<Staff>{return this.api.post('staff',body);}
  updateStaff(id:number,body:unknown):Observable<Staff>{return this.api.put(`staff/${id}`,body);}
  categories(storeId?:number):Observable<Category[]>{return this.api.get(`store-categories${storeId?`?storeId=${storeId}`:''}`);}
  createCategory(body:unknown):Observable<Category>{return this.api.post('store-categories',body);}
  updateCategory(id:number,body:unknown):Observable<Category>{return this.api.put(`store-categories/${id}`,body);}
  voiceSetting(storeId:number):Observable<VoiceSetting>{return this.api.get(`stores/${storeId}/voice-command-setting`);}
  saveVoiceSetting(storeId:number,body:unknown):Observable<VoiceSetting>{return this.api.put(`stores/${storeId}/voice-command-setting`,body);}
}
