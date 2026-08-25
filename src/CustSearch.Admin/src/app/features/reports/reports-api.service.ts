import { Injectable, inject } from '@angular/core';
import { TenantApiClient } from '../../core/api/tenant-api.client';

export interface ReportCatalogItem{reportType:number;code:string;name:string;scope:number;requiredPermission:string;supportsPaging:boolean;}
export interface ReportFilter{fromUtc:string;toUtc:string;storeIds:number[];page:number;pageSize:number;}
export interface ReportDataRow{domain:string;storeId:number|null;metric:string;value:number;label:string|null;occurredUtc:string|null;}
export interface ReportResult{reportType:number;fromUtc:string;toUtc:string;page:number;pageSize:number;totalRows:number;rows:ReportDataRow[];}
export interface ExportJob{id:number;reportType:number;format:number;status:number;progress:number;createdUtc:string;startedUtc:string|null;completedUtc:string|null;expiresUtc:string;error:string|null;attemptCount:number;canDownload:boolean;}
export interface DownloadTicket{token:string;expiresUtc:string;}

@Injectable({providedIn:'root'})
export class ReportsApiService{
  private readonly api=inject(TenantApiClient);
  catalog(){return this.api.get<ReportCatalogItem[]>('reports/catalog');}
  run(reportType:number,filter:ReportFilter){return this.api.post<ReportResult>(`reports/run/${reportType}`,filter);}
  queue(reportType:number,format:number,filter:ReportFilter){return this.api.post<ExportJob>('reports/exports',{reportType,format,filter});}
  jobs(){return this.api.get<ExportJob[]>('reports/exports');}
  retry(jobId:number){return this.api.post<ExportJob>(`reports/exports/${jobId}/retry`);}
  ticket(jobId:number){return this.api.post<DownloadTicket>(`reports/exports/${jobId}/download-ticket`);}
  downloadUrl(jobId:number,token:string){return `/api/tenant/reports/exports/${jobId}/download?token=${encodeURIComponent(token)}`;}
}
