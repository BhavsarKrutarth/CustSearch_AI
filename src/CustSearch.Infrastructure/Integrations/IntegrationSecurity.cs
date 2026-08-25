using CustSearch.Application.Integrations;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CustSearch.Infrastructure.Integrations;

internal static class IntegrationSecurity
{
    private static readonly string[] SensitiveNames=["password","secret","token","credential","authorization","faceembedding","biometric"];
    public static string Hash(ReadOnlySpan<byte>payload)=>Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant();
    public static string ValidateSafePayload(string payloadJson,int maximumBytes=32000)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(payloadJson);if(Encoding.UTF8.GetByteCount(payloadJson)>maximumBytes)throw new IntegrationException("Payload metadata exceeds the allowed size.",IntegrationFailureKind.Validation);
        try{using var document=JsonDocument.Parse(payloadJson,new JsonDocumentOptions{MaxDepth=32});if(document.RootElement.ValueKind!=JsonValueKind.Object)throw new IntegrationException("Payload metadata must be a JSON object.",IntegrationFailureKind.Validation);RejectSensitive(document.RootElement);return document.RootElement.GetRawText();}
        catch(JsonException){throw new IntegrationException("Payload metadata is invalid JSON.",IntegrationFailureKind.Validation);}
    }
    public static string Hint(string reference){var value=reference.Trim();var suffix=value.Length<=4?value:value[^4..];return $"••••{suffix}";}
    private static void RejectSensitive(JsonElement element)
    {
        if(element.ValueKind==JsonValueKind.Object)foreach(var property in element.EnumerateObject()){var normalized=property.Name.Replace("_",string.Empty,StringComparison.Ordinal).Replace("-",string.Empty,StringComparison.Ordinal);if(SensitiveNames.Any(x=>normalized.Contains(x,StringComparison.OrdinalIgnoreCase)))throw new IntegrationException("Payload metadata contains a prohibited sensitive field.",IntegrationFailureKind.Validation);RejectSensitive(property.Value);}
        else if(element.ValueKind==JsonValueKind.Array)foreach(var item in element.EnumerateArray())RejectSensitive(item);
    }
}
