import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';

export interface ReportCatalogItem{reportType:number;code:string;name:string;scope:number;requiredPermission:string;supportsPaging:boolean;}
export interface ReportFilter{fromUtc:string;toUtc:string;storeIds:number[];page:number;pageSize:number;}
export interface ReportDataRow{domain:string;storeId:number|null;metric:string;value:number;label:string|null;occurredUtc:string|null;}
export interface ReportResult{reportType:number;fromUtc:string;toUtc:string;page:number;pageSize:number;totalRows:number;rows:ReportDataRow[];}
export interface ExportJob{id:number;reportType:number;format:number;status:number;progress:number;createdUtc:string;startedUtc:string|null;completedUtc:string|null;expiresUtc:string;error:string|null;attemptCount:number;canDownload:boolean;}
export interface DownloadTicket{token:string;expiresUtc:string;}

@Injectable({providedIn:'root'})
export class ReportsApiService{
  private readonly http=inject(HttpClient);
  private base(platform:boolean){return `/api/${platform?'platform':'tenant'}/reports`;}
  catalog(platform=false){return this.http.get<ReportCatalogItem[]>(`${this.base(platform)}/catalog`);}
  run(reportType:number,filter:ReportFilter,platform=false){return this.http.post<ReportResult>(`${this.base(platform)}/run/${reportType}`,filter);}
  queue(reportType:number,format:number,filter:ReportFilter,platform=false){return this.http.post<ExportJob>(`${this.base(platform)}/exports`,{reportType,format,filter});}
  jobs(platform=false){return this.http.get<ExportJob[]>(`${this.base(platform)}/exports`);}
  retry(jobId:number,platform=false){return this.http.post<ExportJob>(`${this.base(platform)}/exports/${jobId}/retry`,{});}
  ticket(jobId:number,platform=false){return this.http.post<DownloadTicket>(`${this.base(platform)}/exports/${jobId}/download-ticket`,{});}
  downloadUrl(jobId:number,token:string,platform=false){return `${this.base(platform)}/exports/${jobId}/download?token=${encodeURIComponent(token)}`;}
}
