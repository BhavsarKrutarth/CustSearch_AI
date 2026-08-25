import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { TenantApiClient } from '../../core/api/tenant-api.client';

export type IntegrationType=1|2|3|4;
export type IntegrationDirection=1|2;
export type IntegrationDeliveryStatus=1|2|3|4|5;
export interface IntegrationConfigurationView{id:number;provider:string;integrationType:IntegrationType;enabled:boolean;endpointBaseUrl:string;hasCredentialReference:boolean;credentialReferenceHint:string|null;hasWebhookSigningSecret:boolean;webhookSigningSecretHint:string|null;timeoutSeconds:number;retryMaxAttempts:number;retryBaseDelaySeconds:number;createdUtc:string;updatedUtc:string;connectionStatus:string;webhookStatus:string;}
export interface SaveIntegrationRequest{provider:string;integrationType:IntegrationType;enabled:boolean;endpointBaseUrl:string;credentialReference?:string|null;webhookSigningSecretReference?:string|null;timeoutSeconds:number;retryMaxAttempts:number;retryBaseDelaySeconds:number;}
export interface RotateReferencesRequest{credentialReference?:string|null;webhookSigningSecretReference?:string|null;signingGraceMinutes:number;}
export interface IntegrationDeliveryLogView{id:number;integrationConfigurationId:number;inboundEventId:number|null;outboxMessageId:number|null;correlationId:string;provider:string;direction:IntegrationDirection;status:IntegrationDeliveryStatus;durationMilliseconds:number;httpStatusCode:number|null;errorCategory:string|null;createdUtc:string;}
export interface IntegrationOutboxView{id:number;integrationConfigurationId:number;provider:string;destination:string;eventType:string;contractVersion:number;status:number;attemptCount:number;maxAttempts:number;nextAttemptUtc:string;lastResponseCode:number|null;lastError:string|null;correlationId:string;idempotencyKey:string;createdUtc:string;deliveredUtc:string|null;}

/** Tenant integration client. Responses expose masked hints only and no method accepts TenantId. */
@Injectable({providedIn:'root'})
export class IntegrationsApiService{
  private readonly api=inject(TenantApiClient);
  list():Observable<IntegrationConfigurationView[]>{return this.api.get<IntegrationConfigurationView[]>('integrations');}
  get(id:number):Observable<IntegrationConfigurationView>{return this.api.get<IntegrationConfigurationView>(`integrations/${id}`);}
  create(request:SaveIntegrationRequest):Observable<IntegrationConfigurationView>{return this.api.post<IntegrationConfigurationView>('integrations',request);}
  update(id:number,request:SaveIntegrationRequest):Observable<IntegrationConfigurationView>{return this.api.put<IntegrationConfigurationView>(`integrations/${id}`,request);}
  rotate(id:number,request:RotateReferencesRequest):Observable<IntegrationConfigurationView>{return this.api.post<IntegrationConfigurationView>(`integrations/${id}/rotate-references`,request);}
  history(integrationId?:number,take=100):Observable<IntegrationDeliveryLogView[]>{const query=integrationId?`?integrationId=${integrationId}&take=${take}`:`?take=${take}`;return this.api.get<IntegrationDeliveryLogView[]>(`integrations/deliveries${query}`);}
  retry(outboxMessageId:number):Observable<IntegrationOutboxView>{return this.api.post<IntegrationOutboxView>(`integrations/deliveries/${outboxMessageId}/retry`,{});}
  testDelivery(id:number):Observable<IntegrationOutboxView>{return this.api.post<IntegrationOutboxView>(`integrations/${id}/test-delivery`,{});}
}
