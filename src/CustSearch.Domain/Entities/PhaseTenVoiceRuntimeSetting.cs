namespace CustSearch.Domain.Entities;

/// <summary>Phase 10 one-to-one runtime extension of the Phase 5 StoreVoiceCommandSetting; trigger/aliases remain in the Phase 5 master.</summary>
public sealed class StoreVoiceCommandRuntimeSetting
{
    private StoreVoiceCommandRuntimeSetting() { }
    private StoreVoiceCommandRuntimeSetting(long tenantId,long storeId,string languageCode,bool requireConfirmation,int listeningTimeoutSeconds,decimal minimumRecognitionConfidence,DateTime utcNow)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tenantId);ArgumentOutOfRangeException.ThrowIfNegativeOrZero(storeId);
        ArgumentOutOfRangeException.ThrowIfLessThan(listeningTimeoutSeconds,3);ArgumentOutOfRangeException.ThrowIfGreaterThan(listeningTimeoutSeconds,120);
        if(minimumRecognitionConfidence is <0 or >100)throw new ArgumentOutOfRangeException(nameof(minimumRecognitionConfidence));
        TenantId=tenantId;StoreId=storeId;LanguageCode=Require(languageCode,20);RequireConfirmation=requireConfirmation;ListeningTimeoutSeconds=listeningTimeoutSeconds;MinimumRecognitionConfidence=minimumRecognitionConfidence;CreatedUtc=RequireUtc(utcNow,nameof(utcNow));UpdatedUtc=CreatedUtc;
    }
    public long StoreId { get; private set; } public long TenantId { get; private set; } public string LanguageCode { get; private set; }=string.Empty;
    public bool RequireConfirmation { get; private set; } public int ListeningTimeoutSeconds { get; private set; } public decimal MinimumRecognitionConfidence { get; private set; }
    public DateTime CreatedUtc { get; private set; } public DateTime UpdatedUtc { get; private set; }
    public static StoreVoiceCommandRuntimeSetting Create(long tenantId,long storeId,string languageCode,bool requireConfirmation,int listeningTimeoutSeconds,decimal minimumRecognitionConfidence,DateTime utcNow)=>new(tenantId,storeId,languageCode,requireConfirmation,listeningTimeoutSeconds,minimumRecognitionConfidence,utcNow);
    public void Update(string languageCode,bool requireConfirmation,int listeningTimeoutSeconds,decimal minimumRecognitionConfidence,DateTime utcNow)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(listeningTimeoutSeconds,3);ArgumentOutOfRangeException.ThrowIfGreaterThan(listeningTimeoutSeconds,120);if(minimumRecognitionConfidence is <0 or >100)throw new ArgumentOutOfRangeException(nameof(minimumRecognitionConfidence));
        LanguageCode=Require(languageCode,20);RequireConfirmation=requireConfirmation;ListeningTimeoutSeconds=listeningTimeoutSeconds;MinimumRecognitionConfidence=minimumRecognitionConfidence;UpdatedUtc=RequireUtc(utcNow,nameof(utcNow));
    }
    private static string Require(string value,int max){ArgumentException.ThrowIfNullOrWhiteSpace(value);var v=value.Trim();return v.Length<=max?v:throw new ArgumentOutOfRangeException(nameof(value));}
    private static DateTime RequireUtc(DateTime value,string name)=>value.Kind==DateTimeKind.Utc?value:throw new ArgumentException("Timestamp must be UTC.",name);
}
