using System.Reflection;
using System.Text.Json;
using CustSearch.API.AlertsRealtime;
using CustSearch.API.Controllers;
using CustSearch.API.Security;
using CustSearch.Application.Authorization;
using CustSearch.Application.Recognition;
using Microsoft.AspNetCore.Mvc;

namespace CustSearch.IntegrationTests;

public sealed class PhaseFourteenApiContractTests
{
    private static readonly JsonSerializerOptions JsonOptions=new(JsonSerializerDefaults.Web);
    [Fact]public void BrowserTenantIdAndUnknownIdentityFieldsAreRejected(){var injected="""{"consentType":1,"purpose":"welcome","grantedUtc":"2026-08-25T00:00:00Z","consentVersion":"v1","tenantId":999}""";Assert.Throws<JsonException>(()=>JsonSerializer.Deserialize<GrantRecognitionConsentRequest>(injected,JsonOptions));var raw="""{"storeId":1,"consentId":2,"purpose":"welcome","derivedTemplateBase64":"ZGVyaXZlZA==","templateVersion":"v1","rawImage":"secret"}""";Assert.Throws<JsonException>(()=>JsonSerializer.Deserialize<EnrollBiometricTemplateRequest>(raw,JsonOptions));Assert.NotNull(typeof(RecognitionController).GetCustomAttribute<RejectClientTenantIdAttribute>());}
    [Theory][InlineData(nameof(RecognitionController.Consents),PermissionCatalog.Operations.RecognitionView)][InlineData(nameof(RecognitionController.Grant),PermissionCatalog.Operations.RecognitionConsentManage)][InlineData(nameof(RecognitionController.Withdraw),PermissionCatalog.Operations.RecognitionConsentManage)][InlineData(nameof(RecognitionController.Templates),PermissionCatalog.Operations.RecognitionView)][InlineData(nameof(RecognitionController.Enroll),PermissionCatalog.Operations.RecognitionEnroll)][InlineData(nameof(RecognitionController.CreateCandidate),PermissionCatalog.Operations.RecognitionSettingsManage)][InlineData(nameof(RecognitionController.Candidates),PermissionCatalog.Operations.RecognitionView)][InlineData(nameof(RecognitionController.Review),PermissionCatalog.Operations.RecognitionReview)]public void EndpointsRequireExactRecognitionPermission(string method,string permission){var attribute=typeof(RecognitionController).GetMethod(method)!.GetCustomAttribute<HasPermissionAttribute>();Assert.Equal(AuthorizationPolicyNames.ForPermission(permission),attribute?.Policy);}
    [Fact]public void PublicViewsNeverExposeCiphertextOrEncryptionKey(){var template=typeof(BiometricTemplateView).GetProperties().Select(x=>x.Name).ToArray();Assert.DoesNotContain("EncryptedTemplate",template);Assert.DoesNotContain("Nonce",template);Assert.DoesNotContain("AuthenticationTag",template);Assert.DoesNotContain("EncryptionKeyReference",template);}
    [Fact]public void EnabledConfigurationRequiresAValidSecretSuppliedKey(){var disabled=new RecognitionSecurityOptions();Assert.True(disabled.HasValidEncryptionConfiguration());var enabled=new RecognitionSecurityOptions{Enabled=true,EncryptionKeyReference="vault:key",EncryptionKeyBase64="not-base64"};Assert.False(enabled.HasValidEncryptionConfiguration());enabled.EncryptionKeyBase64=Convert.ToBase64String(new byte[32]);Assert.True(enabled.HasValidEncryptionConfiguration());}
}
