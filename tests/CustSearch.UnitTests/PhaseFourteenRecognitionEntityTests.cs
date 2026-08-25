using CustSearch.Domain.Entities;
using CustSearch.Domain.Enums;

namespace CustSearch.UnitTests;

public sealed class PhaseFourteenRecognitionEntityTests
{
    private static readonly DateTime Now=new(2026,8,25,12,0,0,DateTimeKind.Utc);
    [Fact]public void ConsentIsPurposeSpecificExpirableAndWithdrawable(){var consent=CustomerRecognitionConsent.Grant(1,2,RecognitionConsentType.BiometricRecognition,"Store welcome",Now,Now.AddDays(30),"2026-01",3,"consent:2");Assert.True(consent.IsActiveAt(Now.AddDays(1)));Assert.False(consent.IsActiveAt(Now.AddDays(31)));consent.Withdraw(Now.AddDays(2));Assert.False(consent.IsActiveAt(Now.AddDays(2)));Assert.Throws<InvalidOperationException>(()=>consent.Withdraw(Now.AddDays(3)));}
    [Fact]public void TemplateRequiresProtectedMaterialAndErasesItOnWithdrawal(){var template=BiometricTemplate.Enroll(1,2,3,4,new byte[64],new byte[12],new byte[16],"vault:key","AES-256-GCM","onnx-v1",Now);template.DisableAndErase(Now.AddMinutes(1),Now.AddDays(30));Assert.Equal(BiometricTemplateStatus.Deleted,template.Status);Assert.Empty(template.EncryptedTemplate);Assert.Empty(template.Nonce);Assert.Empty(template.AuthenticationTag);}
    [Fact]public void CandidateIsOnlyAReviewableClaim(){var candidate=RecognitionCandidate.Create(1,2,3,4,5,"request-1","Store welcome",.91m,.88m,.89m,RecognitionCandidateStatus.Ambiguous,"Review",Now);candidate.Review(true,6,"Verified by operator",Now.AddMinutes(1));Assert.Equal(RecognitionCandidateStatus.Accepted,candidate.Status);Assert.Equal(5,candidate.CustomerId);Assert.Equal(3,candidate.PersonTrackSessionId);Assert.Throws<InvalidOperationException>(()=>candidate.Review(false,6,"Again",Now.AddMinutes(2)));}
    [Fact]public void RawOrMalformedProtectedTemplateCannotBeCreated(){Assert.Throws<ArgumentException>(()=>BiometricTemplate.Enroll(1,2,3,4,[],new byte[12],new byte[16],"vault:key","AES-256-GCM","v1",Now));Assert.Throws<ArgumentException>(()=>BiometricTemplate.Enroll(1,2,3,4,new byte[32],new byte[11],new byte[16],"vault:key","AES-256-GCM","v1",Now));}
}
