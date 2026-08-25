import { HttpClient, HttpParams, HttpResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

export interface ReportCatalogItem{reportType:string;name:string;description:string;requiredPermission:string;supportsStoreFilter:boolean;supportsDateFilter:boolean;}
export interface ReportDataView{columns:string[];rows:Record<string,unknown>[];}
export interface ReportExportJobView{id:number;tenantId:number|null;requestedByUserId:number;reportType:string;format:number;status:number;progressPercent:number;downloadFileName:string|null;contentType:string|null;contentLength:number|null;sha256:string|null;errorMessage:string|null;requestedUtc:string;startedUtc:string|null;completedUtc:string|null;expiresUtc:string|null;attemptCount:number;}
export interface ReportFilters{storeId?:number;tenantId?:number;fromUtc?:string;toUtc?:string;}

/** Typed Phase 15 client. Tenant calls deliberately omit TenantId from query and body payloads. */
@Injectable({providedIn:'root'})
export class ReportsApiService{
  private readonly http=inject(HttpClient);
  catalog(platform:boolean):Observable<ReportCatalogItem[]>{return this.http.get<ReportCatalogItem[]>(`${this.base(platform)}/catalog`);}
  preview(platform:boolean,reportType:string,filters:ReportFilters):Observable<ReportDataView>{let params=new HttpParams().set('reportType',reportType);for(const [key,value]of Object.entries(filters))if(value!==undefined&&value!==null&&value!=='')params=params.set(key,String(value));return this.http.get<ReportDataView>(`${this.base(platform)}/preview`,{params});}
  queue(platform:boolean,reportType:string,format:number,filters:ReportFilters):Observable<ReportExportJobView>{const common={reportType,format,fromUtc:filters.fromUtc??null,toUtc:filters.toUtc??null};const body=platform?{...common,tenantId:filters.tenantId??null,storeId:null}:{...common,storeId:filters.storeId??null};return this.http.post<ReportExportJobView>(`${this.base(platform)}/exports`,body);}
  jobs(platform:boolean):Observable<ReportExportJobView[]>{return this.http.get<ReportExportJobView[]>(`${this.base(platform)}/exports?take=100`);}
  download(platform:boolean,id:number):Observable<HttpResponse<Blob>>{return this.http.get(`${this.base(platform)}/exports/${id}/download`,{observe:'response',responseType:'blob'});}
  private base(platform:boolean):string{return platform?'/api/platform/reports':'/api/tenant/reports';}
}

