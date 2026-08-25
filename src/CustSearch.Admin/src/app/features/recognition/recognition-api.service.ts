import { Injectable, inject } from '@angular/core';
import { TenantApiClient } from '../../core/api/tenant-api.client';

export interface RecognitionConsent {id:number;customerId:number;consentType:number;purpose:string;grantedUtc:string;expiresUtc:string|null;withdrawnUtc:string|null;consentVersion:string;capturedByUserId:number;evidenceReference:string|null;isActive:boolean;}
export interface BiometricTemplateMetadata {id:number;storeId:number;customerId:number;consentId:number;algorithm:string;templateVersion:string;status:number;createdUtc:string;disabledUtc:string|null;deletedUtc:string|null;retentionUntilUtc:string|null;}
export interface RecognitionCandidate {id:number;storeId:number;personTrackSessionId:number;biometricTemplateId:number;customerId:number;requestId:string;purpose:string;confidence:number;quality:number;secondBestConfidence:number|null;status:number;createdUtc:string;reviewedUtc:string|null;reviewedByUserId:number|null;reviewReason:string|null;}
export interface RecognitionSettings {enabled:boolean;minimumConfidence:number;minimumQuality:number;ambiguityDelta:number;retentionDaysAfterWithdrawal:number;storesRawImages:false;automaticIdentityMerge:false;externalIdentityDatabases:false;}

@Injectable({providedIn:'root'})
export class RecognitionApiService{
  private readonly api=inject(TenantApiClient);
  settings(){return this.api.get<RecognitionSettings>('recognition/settings');}
  consents(customerId:number){return this.api.get<RecognitionConsent[]>(`recognition/customers/${customerId}/consents`);}
  grantConsent(customerId:number,body:{consentType:number;purpose:string;grantedUtc:string;expiresUtc:string|null;consentVersion:string;evidenceReference:string|null}){return this.api.post<RecognitionConsent>(`recognition/customers/${customerId}/consents`,body);}
  withdrawConsent(consentId:number,reason:string){return this.api.post<RecognitionConsent>(`recognition/consents/${consentId}/withdraw`,{reason});}
  templates(customerId:number){return this.api.get<BiometricTemplateMetadata[]>(`recognition/customers/${customerId}/templates`);}
  enroll(customerId:number,body:{storeId:number;consentId:number;purpose:string;derivedTemplateBase64:string;templateVersion:string}){return this.api.post<BiometricTemplateMetadata>(`recognition/customers/${customerId}/templates`,body);}
  candidates(storeId?:number,status?:number){const params=new URLSearchParams();if(storeId)params.set('storeId',String(storeId));if(status)params.set('status',String(status));const query=params.size?`?${params}`:'';return this.api.get<RecognitionCandidate[]>(`recognition/candidates${query}`);}
  review(candidateId:number,accept:boolean,reason:string){return this.api.post<RecognitionCandidate>(`recognition/candidates/${candidateId}/review`,{accept,reason});}
}
