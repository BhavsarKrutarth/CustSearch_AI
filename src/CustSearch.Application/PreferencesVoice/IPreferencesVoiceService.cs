using CustSearch.Application.TenantOperations;
using CustSearch.Domain.Enums;

namespace CustSearch.Application.PreferencesVoice;

/// <summary>Phase 10 application boundary. Tenant/store/actor identity is resolved from authenticated server context.</summary>
public interface IPreferencesVoiceService
{
    Task<CustomerPreferencesView> GetCustomerPreferencesAsync(long customerId,CancellationToken cancellationToken=default);
    Task<CustomerPreferencesView> AddCustomerTagAsync(long customerId,AddCustomerPreferenceCommand command,TenantAuditContext audit,CancellationToken cancellationToken=default);
    Task<CustomerPreferencesView> RecalculateCustomerAsync(long customerId,TenantAuditContext audit,CancellationToken cancellationToken=default);
    Task<HouseholdPreferencesView> GetHouseholdPreferencesAsync(long householdId,CancellationToken cancellationToken=default);
    Task<HouseholdPreferencesView> AddHouseholdTagAsync(long householdId,AddHouseholdPreferenceTagCommand command,TenantAuditContext audit,CancellationToken cancellationToken=default);
    Task<VoiceSettingView> GetVoiceSettingAsync(long storeId,CancellationToken cancellationToken=default);
    Task<VoiceSettingView> SaveVoiceSettingAsync(long storeId,SaveVoiceRuntimeSettingCommand command,TenantAuditContext audit,CancellationToken cancellationToken=default);
    Task<VoiceSessionView> StartVoiceSessionAsync(StartVoiceSessionCommand command,TenantAuditContext audit,CancellationToken cancellationToken=default);
    Task<VoiceSessionView> InterpretVoiceSessionAsync(long sessionId,InterpretVoiceSessionCommand command,TenantAuditContext audit,CancellationToken cancellationToken=default);
    Task<VoiceSessionView> ConfirmVoiceSessionAsync(long sessionId,TenantAuditContext audit,CancellationToken cancellationToken=default);
    Task<VoiceSessionView> RejectVoiceSessionAsync(long sessionId,TenantAuditContext audit,CancellationToken cancellationToken=default);
    Task<IReadOnlyList<PreferenceAuditItem>> GetAuditHistoryAsync(long? customerId,long? storeId,int take=100,CancellationToken cancellationToken=default);
}

public sealed record AddCustomerPreferenceCommand(long StoreId,PreferenceType PreferenceType,long? ReferenceId,string? Value,decimal? SignalScore,decimal? Confidence,string? Reason);
public sealed record AddHouseholdPreferenceTagCommand(PreferenceType PreferenceType,long? ReferenceId,string Value,HouseholdPreferenceTagSource Source,string? Reason);
public sealed record SaveVoiceRuntimeSettingCommand(string TriggerKeyword,string ResponseMode,bool IsEnabled,bool RequireConfirmationForAmbiguousCategory,IReadOnlyList<string> Aliases,string LanguageCode,bool RequireConfirmation,int ListeningTimeoutSeconds,decimal MinimumRecognitionConfidence);
public sealed record StartVoiceSessionCommand(long StoreId,long CustomerId,string TriggerText);
public sealed record InterpretVoiceSessionCommand(string RecognizedText,decimal RecognitionConfidence,PreferenceType PreferenceType,long? ReferenceId,string? Value,string? Reason);

public sealed record PreferenceSignalView(long Id,long? StoreId,PreferenceType PreferenceType,long? ReferenceId,string? Value,decimal? SignalScore,PreferenceSignalSource Source,decimal? Confidence,DateTime FirstObservedUtc,DateTime LastObservedUtc,bool IsActive,string? Reason);
public sealed record PreferenceScoreView(long Id,PreferenceType PreferenceType,long? ReferenceId,string? Value,decimal Score,long WeightVersionId,DateTime CalculatedUtc);
public sealed record CustomerPreferencesView(long CustomerId,string CustomerCode,string CustomerName,IReadOnlyList<PreferenceSignalView> Signals,IReadOnlyList<PreferenceScoreView> Scores);
public sealed record HouseholdMemberPreferenceView(long CustomerId,string CustomerName,IReadOnlyList<PreferenceScoreView> Scores);
public sealed record HouseholdTagView(long Id,PreferenceType PreferenceType,long? ReferenceId,string Value,HouseholdPreferenceTagSource Source,string? Reason,DateTime CreatedUtc);
public sealed record HouseholdPreferencesView(long HouseholdId,string HouseholdName,IReadOnlyList<HouseholdMemberPreferenceView> VerifiedMembers,IReadOnlyList<PreferenceScoreView> AggregateScores,IReadOnlyList<HouseholdTagView> SharedTags);
public sealed record VoiceSettingView(long StoreId,string TriggerKeyword,string ResponseMode,bool IsEnabled,bool RequireConfirmationForAmbiguousCategory,IReadOnlyList<string> Aliases,string LanguageCode,bool RequireConfirmation,int ListeningTimeoutSeconds,decimal MinimumRecognitionConfidence);
public sealed record VoiceSessionView(long Id,long StoreId,long CustomerId,string MatchedTrigger,string? RecognizedText,decimal? RecognitionConfidence,PreferenceType? ProposedPreferenceType,long? ProposedReferenceId,string? ProposedValue,bool ConfirmationRequired,VoiceCommandSessionStatus Status,DateTime ExpiresUtc,DateTime? ResolvedUtc);
public sealed record PreferenceAuditItem(long Id,long? StoreId,long? UserId,string Action,string EntityType,string? EntityId,string? BeforeJson,string? AfterJson,string CorrelationId,DateTime CreatedUtc);

/// <summary>Phase 10 business-rule error mapped by API middleware to a safe client response.</summary>
public sealed class PreferenceBusinessRuleException(string message):Exception(message);
