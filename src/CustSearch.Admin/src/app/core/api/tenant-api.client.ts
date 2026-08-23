import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { PageQuery, PageResponse } from '../auth/auth.models';

/** Sends typed same-origin tenant requests without accepting a browser-supplied TenantId. */
@Injectable({ providedIn: 'root' })
export class TenantApiClient {
  private readonly http = inject(HttpClient);

  get<T>(relativePath: string): Observable<T> { return this.http.get<T>(this.tenantUrl(relativePath)); }
  post<T>(relativePath: string, body: unknown = {}): Observable<T> { return this.http.post<T>(this.tenantUrl(relativePath), body); }
  put<T>(relativePath: string, body: unknown): Observable<T> { return this.http.put<T>(this.tenantUrl(relativePath), body); }

  getPage<T>(relativePath: string, query: PageQuery): Observable<PageResponse<T>> {
    let params = new HttpParams().set('pageNumber', query.pageNumber).set('pageSize', query.pageSize);
    if (query.search) params = params.set('search', query.search);
    if (query.sortBy) params = params.set('sortBy', query.sortBy);
    if (query.sortDirection) params = params.set('sortDirection', query.sortDirection);
    for (const [name, value] of Object.entries(query.filters ?? {})) {
      if (name.toLowerCase() === 'tenantid') throw new Error('TenantId cannot be supplied by the browser.');
      params = params.set(name, String(value));
    }
    return this.http.get<PageResponse<T>>(this.tenantUrl(relativePath), { params });
  }

  private tenantUrl(relativePath: string): string {
    const safePath = relativePath.replace(/^\/+/, '');
    if (!safePath || safePath.includes('..') || /^https?:/i.test(safePath)) throw new Error('Tenant API path must be a safe relative path.');
    return `/api/tenant/${safePath}`;
  }
}
