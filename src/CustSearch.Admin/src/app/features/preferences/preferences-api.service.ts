import { Injectable, inject } from '@angular/core';
import { TenantApiClient } from '../../core/api/tenant-api.client';

export type PreferenceType=1|2|3|4|5;
export type PreferenceSource=1|2|3|4;
export interface PreferenceSignal { id:number;storeId:number|null;preferenceType:PreferenceType;referenceId:number|null;value:string|null;signalScore:number|null;source:PreferenceSource;confidence:number|null;firstObservedUtc:string;lastObservedUtc:string;isActive:boolean;reason:string|null; }
export interface PreferenceScore { id:number;preferenceType:PreferenceType;referenceId:number|null;value:string|null;score:number;weightVersionId:number;calculatedUtc:string; }
export interface CustomerPreferences { customerId:number;customerCode:string;customerName:string;signals:PreferenceSignal[];scores:PreferenceScore[]; }
export interface HouseholdPreferences { householdId:number;householdName:string;verifiedMembers:{customerId:number;customerName:string;scores:PreferenceScore[]}[];aggregateScores:PreferenceScore[];sharedTags:{id:number;preferenceType:PreferenceType;referenceId:number|null;value:string;source:number;reason:string|null;createdUtc:string}[]; }
export interface PreferenceWeight { id:number;versionCode:string;manualStaffWeight:number;purchaseWeight:number;categoryInteractionWeight:number;voiceConfirmedWeight:number;isActive:boolean;createdUtc:string; }
export interface VoiceSetting { storeId:number;triggerKeyword:string;responseMode:string;isEnabled:boolean;requireConfirmationForAmbiguousCategory:boolean;aliases:string[];languageCode:string;requireConfirmation:boolean;listeningTimeoutSeconds:number;minimumRecognitionConfidence:number; }
export interface ProductCategoryAlias { id:number;storeId:number|null;productCategoryId:number;aliasText:string;normalizedAliasText:string;languageCode:string;isActive:boolean;createdUtc:string; }
export interface VoiceCategoryCandidate { categoryId:number;categoryCode:string;categoryName:string;matchSource:string; }
export interface VoiceSession { id:number;storeId:number;customerId:number;matchedTrigger:string;recognizedText:string|null;recognitionConfidence:number|null;proposedPreferenceType:PreferenceType|null;proposedReferenceId:number|null;proposedValue:string|null;confirmationRequired:boolean;status:number;expiresUtc:string;resolvedUtc:string|null; }
export interface VoiceInterpretResult { session:VoiceSession;needsCategorySelection:boolean;candidates:VoiceCategoryCandidate[];resolutionMessage:string|null; }
export interface PreferenceAudit { id:number;storeId:number|null;userId:number|null;action:string;entityType:string;entityId:string|null;beforeJson:string|null;afterJson:string|null;correlationId:string;createdUtc:string; }

@Injectable({providedIn:'root'})
export class PreferencesApiService {
  private readonly api=inject(TenantApiClient);
  customer(customerId:number){return this.api.get<CustomerPreferences>(`customers/${customerId}/preferences`);}
  addCustomerTag(customerId:number,body:{storeId:number;preferenceType:PreferenceType;referenceId:number|null;value:string|null;signalScore:number|null;confidence:number|null;reason:string|null}){return this.api.post<CustomerPreferences>(`customers/${customerId}/preferences/tags`,body);}
  recalculateCustomer(customerId:number){return this.api.post<CustomerPreferences>(`customers/${customerId}/preferences/recalculate`);}
  household(householdId:number){return this.api.get<HouseholdPreferences>(`households/${householdId}/preferences`);}
  addHouseholdTag(householdId:number,body:{preferenceType:PreferenceType;referenceId:number|null;value:string;source:number;reason:string|null}){return this.api.post<HouseholdPreferences>(`households/${householdId}/preferences/tags`,body);}
  activeWeights(){return this.api.get<PreferenceWeight>('preferences/weights/active');}
  saveWeights(body:{versionCode:string;manualStaffWeight:number;purchaseWeight:number;categoryInteractionWeight:number;voiceConfirmedWeight:number}){return this.api.post<PreferenceWeight>('preferences/weights',body);}
  voiceSetting(storeId:number){return this.api.get<VoiceSetting>(`stores/${storeId}/voice-command-runtime`);}
  saveVoiceSetting(storeId:number,body:{triggerKeyword:string;responseMode:string;isEnabled:boolean;requireConfirmationForAmbiguousCategory:boolean;aliases:string[];languageCode:string;requireConfirmation:boolean;listeningTimeoutSeconds:number;minimumRecognitionConfidence:number}){return this.api.put<VoiceSetting>(`stores/${storeId}/voice-command-runtime`,body);}
  categoryAliases(categoryId:number,storeId?:number){const query=storeId?`?storeId=${storeId}`:'';return this.api.get<ProductCategoryAlias[]>(`store-categories/${categoryId}/aliases${query}`);}
  addCategoryAlias(categoryId:number,body:{storeId:number|null;aliasText:string;languageCode:string}){return this.api.post<ProductCategoryAlias>(`store-categories/${categoryId}/aliases`,body);}
  startVoice(body:{storeId:number;customerId:number;triggerText:string}){return this.api.post<VoiceSession>('voice/commands/start',body);}
  interpretVoice(sessionId:number,body:{recognizedText:string;recognitionConfidence:number;selectedCategoryId:number|null;reason:string|null}){return this.api.post<VoiceInterpretResult>(`voice/commands/${sessionId}/interpret`,body);}
  confirmVoice(sessionId:number){return this.api.post<VoiceSession>(`voice/commands/${sessionId}/confirm`);}
  rejectVoice(sessionId:number){return this.api.post<VoiceSession>(`voice/commands/${sessionId}/reject`);}
  audit(customerId?:number,storeId?:number){const q=new URLSearchParams();if(customerId)q.set('customerId',String(customerId));if(storeId)q.set('storeId',String(storeId));return this.api.get<PreferenceAudit[]>(`preferences/audit${q.size?'?'+q.toString():''}`);}
}
