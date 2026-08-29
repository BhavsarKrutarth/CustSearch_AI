using System.Text;
using System.Security.Cryptography;
using System.Text.Json;
using System.Data.Common;
using System.Globalization;
using CustSearch.Application.Abstractions.Data;
using CustSearch.Application.Authentication;
using CustSearch.Application.CamerasTracking;
using CustSearch.Application.Authorization;
using CustSearch.Application.Security;
using CustSearch.Domain.Entities;
using CustSearch.Domain.Enums;
using CustSearch.Infrastructure.Security;
using Microsoft.Extensions.Options;

namespace CustSearch.UnitTests;
public sealed class PhaseEighteenSecurityTests
{
    private static readonly SecurityRiskRule Rule=new(7,70,5000,30,15);
    private static readonly JsonSerializerOptions WebJson=new(JsonSerializerDefaults.Web);
    private static SecurityRiskSignals Signals(bool putBack=false,bool paid=false,bool staff=false,bool handoff=false,bool crowd=false,bool reentry=false,decimal occlusion=1,int gap=0)=>new(true,true,putBack,true,paid,staff,handoff,crowd,reentry,false,.95m,occlusion,gap,6500);

    [Fact]public void PickupThenPutBackIsSuppressed(){var x=SecurityRiskEngine.Evaluate(Rule,Signals(putBack:true));Assert.True(x.Suppressed);Assert.False(x.Candidate);}
    [Fact]public void PaidCheckoutAndExitIsSuppressed(){var x=SecurityRiskEngine.Evaluate(Rule,Signals(paid:true));Assert.True(x.Suppressed);Assert.False(x.Candidate);}
    [Fact]public void PickupWithoutPaymentAndExitCreatesCandidate(){var x=SecurityRiskEngine.Evaluate(Rule,Signals());Assert.True(x.Candidate);Assert.True(x.Score>=Rule.RiskThreshold);}
    [Fact]public void ExitWithoutProbablePossessionNeverCreatesCandidateEvenAtLowThreshold(){var x=SecurityRiskEngine.Evaluate(Rule with{RiskThreshold=1},Signals()with{ProbablePossession=false});Assert.True(x.Suppressed);Assert.False(x.Candidate);}
    [Fact]public void CorrelationUsesOrderedMatchingPutBackAndCheckoutSignals(){var exit=DateTime.SpecifyKind(DateTime.UtcNow,DateTimeKind.Utc);var pickup=new SecurityObservedSignal(SecurityObservationType.ProbablePickup,exit.AddSeconds(-20),.9m,10,5);var result=SecuritySignalCorrelationEngine.Correlate([new(SecurityObservationType.ProbablePutBack,exit.AddSeconds(-25),.8m,10,5),pickup,new(SecurityObservationType.ProbablePutBack,exit.AddSeconds(-15),.8m,11,5),new(SecurityObservationType.CheckoutZoneVisit,exit.AddSeconds(-10),.9m,null,null)],exit);Assert.False(result.PutBackObserved);Assert.True(result.CheckoutVisited);var matched=SecuritySignalCorrelationEngine.Correlate([pickup,new(SecurityObservationType.ProbablePutBack,exit.AddSeconds(-5),.8m,10,5)],exit);Assert.True(matched.PutBackObserved);}
    [Fact]public void StaffRestockingIsSuppressed(){Assert.True(SecurityRiskEngine.Evaluate(Rule,Signals(staff:true)).Suppressed);}
    [Fact]public void GroupHandoffReducesRisk(){Assert.True(SecurityRiskEngine.Evaluate(Rule,Signals(handoff:true)).Score<SecurityRiskEngine.Evaluate(Rule,Signals()).Score);}
    [Fact]public void CrowdOcclusionAndCameraGapReduceRisk(){var clear=SecurityRiskEngine.Evaluate(Rule,Signals()).Score;var degraded=SecurityRiskEngine.Evaluate(Rule,Signals(crowd:true,occlusion:.2m,gap:45)).Score;Assert.True(degraded<clear);}
    [Fact]public void ReentryReducesDuplicateExitRisk(){Assert.True(SecurityRiskEngine.Evaluate(Rule,Signals(reentry:true)).Score<SecurityRiskEngine.Evaluate(Rule,Signals()).Score);}
    [Fact]public void HumanConfirmationRequiresReason(){Assert.Throws<InvalidOperationException>(()=>SecurityIncidentStateMachine.RequireTransition(SecurityIncidentStatus.UnderReview,SecurityIncidentStatus.ConfirmedLoss,true,null));Assert.Throws<InvalidOperationException>(()=>SecurityIncidentStateMachine.RequireTransition(SecurityIncidentStatus.UnderReview,SecurityIncidentStatus.ConfirmedLoss,false,"counted loss"));}
    [Fact]public void RequiredPermissionsAreCatalogued(){var required=new[]{PermissionCatalog.Security.IncidentsView,PermissionCatalog.Security.IncidentsAcknowledge,PermissionCatalog.Security.IncidentsAssign,PermissionCatalog.Security.IncidentsReview,PermissionCatalog.Security.IncidentsConfirmLoss,PermissionCatalog.Security.IncidentsResolve,PermissionCatalog.Security.EvidenceView,PermissionCatalog.Security.EvidenceExport,PermissionCatalog.Security.SettingsView,PermissionCatalog.Security.SettingsManage,PermissionCatalog.Security.RulesView,PermissionCatalog.Security.RulesManage,PermissionCatalog.Security.ReportsView};Assert.All(required,x=>Assert.Contains(x,PermissionCatalog.All));}
    [Fact]public void EvidenceTicketIsUserTenantPurposeBoundAndExpires(){var service=new SecurityEvidenceTokenService(Options.Create(new SecurityEvidenceOptions{DownloadSigningKey=new string('x',32),EncryptionKeyBase64=Convert.ToBase64String(new byte[32])}));var now=DateTime.UtcNow;var ticket=service.Create(3,2,4,1,false,now.AddMinutes(2));service.Validate(ticket.Token,3,2,4,1,false,now);Assert.Throws<SecurityException>(()=>service.Validate(ticket.Token,3,2,5,1,false,now));Assert.Throws<SecurityException>(()=>service.Validate(ticket.Token,3,2,4,1,false,now.AddMinutes(3)));}
    [Fact]public async Task EvidenceStoreRejectsTraversalBeforeFileAccess(){var store=new LocalEncryptedSecurityEvidenceStore(Options.Create(new SecurityEvidenceOptions{StoragePath=Path.GetTempPath(),DownloadSigningKey=new string('x',32),EncryptionKeyBase64=Convert.ToBase64String(Encoding.UTF8.GetBytes("01234567890123456789012345678901"))}));await Assert.ThrowsAsync<SecurityException>(()=>store.OpenDecryptedAsync("../outside.bin"));}
    [Fact]public async Task AiServiceCannotSubmitAnotherTenantEvenWithValidSignature(){const string secret="phase18-unit-service-secret-at-least-32-bytes";var now=DateTime.UtcNow;var request=new SecurityObservationRequest(99,2,3,null,null,"anonymous-track",SecurityObservationType.PersonExit,now,null,null,null,.9m,"test",null);var body=JsonSerializer.SerializeToUtf8Bytes(request,WebJson);var timestamp=new DateTimeOffset(now).ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);var nonce="unique-nonce";var idempotency="unique-request";var hash=Convert.ToHexString(SHA256.HashData(body)).ToLowerInvariant();var signature=Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret),Encoding.UTF8.GetBytes($"{timestamp}\n{nonce}\n{idempotency}\n{hash}")));var service=Service(secret,now);var failure=await Assert.ThrowsAsync<SecurityException>(()=>service.IngestAsync(new("unit",timestamp,nonce,signature,idempotency,body,"test")));Assert.Equal(SecurityFailureKind.Forbidden,failure.Kind);}
    [Fact]public async Task ExpiredAiServiceRequestIsRejectedBeforeDatabase(){var now=DateTime.UtcNow;var service=Service(new string('s',40),now);var failure=await Assert.ThrowsAsync<SecurityException>(()=>service.IngestAsync(new("unit",new DateTimeOffset(now.AddHours(-1)).ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture),"nonce","00","key",Encoding.UTF8.GetBytes("{}"),"test")));Assert.Equal(SecurityFailureKind.Unauthorized,failure.Kind);}
    private static SecurityPlatformService Service(string secret,DateTime now)=>new(new NoDatabase(),new NoCurrentUser(),new SecretResolver(secret),new NullSecurityRealtimePublisher(),null!,null!,new FixedTime(now),Options.Create(new SecurityIngestionOptions()));
    private sealed class NoDatabase:IDbConnectionFactory{public Task<DbConnection>OpenConnectionAsync(CancellationToken cancellationToken=default)=>throw new InvalidOperationException("Database should not be reached.");}
    private sealed class NoCurrentUser:ICurrentUserContext{public bool IsAuthenticated=>false;public long UserId=>0;public long?TenantId=>null;public bool IsPlatformAdmin=>false;public string SecurityStamp=>"";public IReadOnlySet<string>Roles=>new HashSet<string>();public IReadOnlySet<string>Permissions=>new HashSet<string>();public IReadOnlySet<long>StoreIds=>new HashSet<long>();}
    private sealed class SecretResolver(string secret):ICctvServiceSecretResolver{public ValueTask<CctvServiceCredential?>ResolveAsync(string serviceId,CancellationToken cancellationToken=default)=>ValueTask.FromResult<CctvServiceCredential?>(new(secret,1,new HashSet<long>{2}));}
    private sealed class FixedTime(DateTime value):TimeProvider{public override DateTimeOffset GetUtcNow()=>new(value,TimeSpan.Zero);}
}
