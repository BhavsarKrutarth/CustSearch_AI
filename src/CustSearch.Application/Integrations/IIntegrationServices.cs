using CustSearch.Application.TenantOperations;
using CustSearch.Domain.Enums;

namespace CustSearch.Application.Integrations;

public interface IIntegrationManagementService
{
    Task<IReadOnlyList<IntegrationConfigurationView>> ListAsync(CancellationToken cancellationToken=default);
    Task<IntegrationConfigurationView> GetAsync(long integrationId,CancellationToken cancellationToken=default);
    Task<IntegrationConfigurationView> SaveAsync(long?integrationId,SaveIntegrationConfigurationCommand command,TenantAuditContext audit,CancellationToken cancellationToken=default);
    Task<IntegrationConfigurationView> RotateReferencesAsync(long integrationId,RotateIntegrationReferencesCommand command,TenantAuditContext audit,CancellationToken cancellationToken=default);
    Task<IReadOnlyList<IntegrationDeliveryLogView>> DeliveryHistoryAsync(long?integrationId,int take=100,CancellationToken cancellationToken=default);
    Task<IntegrationOutboxView> RetryDeliveryAsync(long deliveryId,TenantAuditContext audit,CancellationToken cancellationToken=default);
    Task<IntegrationOutboxView> QueueOutboundAsync(QueueIntegrationOutboundCommand command,CancellationToken cancellationToken=default);
}

public interface IInboundIntegrationService
{
    Task<InboundIntegrationAcknowledgement> ReceiveAsync(InboundIntegrationRequest request,CancellationToken cancellationToken=default);
}

public interface IIntegrationOutboxProcessor
{
    Task<IntegrationOutboxProcessResult> ProcessDueAsync(int batchSize=50,CancellationToken cancellationToken=default);
}

public sealed record SaveIntegrationConfigurationCommand(string Provider,IntegrationType IntegrationType,bool Enabled,string EndpointBaseUrl,string?CredentialReference,string?WebhookSigningSecretReference,int TimeoutSeconds,int RetryMaxAttempts,int RetryBaseDelaySeconds);
public sealed record RotateIntegrationReferencesCommand(string?CredentialReference,string?WebhookSigningSecretReference,int SigningGraceMinutes);
public sealed record QueueIntegrationOutboundCommand(long IntegrationConfigurationId,string EventType,int ContractVersion,string PayloadMetadataJson,string IdempotencyKey,string CorrelationId);
public sealed record IntegrationConfigurationView(long Id,string Provider,IntegrationType IntegrationType,bool Enabled,string EndpointBaseUrl,bool HasCredentialReference,string?CredentialReferenceHint,bool HasWebhookSigningSecret,string?WebhookSigningSecretHint,int TimeoutSeconds,int RetryMaxAttempts,int RetryBaseDelaySeconds,DateTime CreatedUtc,DateTime UpdatedUtc,string ConnectionStatus,string WebhookStatus);
public sealed record IntegrationDeliveryLogView(long Id,long IntegrationConfigurationId,long?InboundEventId,long?OutboxMessageId,string CorrelationId,string Provider,IntegrationDirection Direction,IntegrationDeliveryStatus Status,long DurationMilliseconds,int?HttpStatusCode,string?ErrorCategory,DateTime CreatedUtc);
public sealed record IntegrationOutboxView(long Id,long IntegrationConfigurationId,string Provider,string Destination,string EventType,int ContractVersion,IntegrationOutboxStatus Status,int AttemptCount,int MaxAttempts,DateTime NextAttemptUtc,int?LastResponseCode,string?LastError,string CorrelationId,string IdempotencyKey,DateTime CreatedUtc,DateTime?DeliveredUtc);
public sealed record InboundIntegrationRequest(long IntegrationConfigurationId,string TenantId,string Timestamp,string Signature,string ProviderEventId,string IdempotencyKey,ReadOnlyMemory<byte>Body,string CorrelationId);
public sealed record InboundIntegrationAcknowledgement(long InboundEventId,bool Duplicate,string Status,string CorrelationId);
public sealed record IntegrationOutboxProcessResult(int Claimed,int Delivered,int Failed,int DeadLettered);

public enum IntegrationFailureKind { Validation,Unauthorized,Forbidden,NotFound,Conflict,Unavailable }
public sealed class IntegrationException(string message,IntegrationFailureKind kind):Exception(message){public IntegrationFailureKind Kind{get;}=kind;}

/// <summary>Resolves opaque references from environment/vault-backed configuration without exposing values to persistence or API responses.</summary>
public interface IIntegrationSecretResolver
{
    ValueTask<string?> ResolveAsync(string reference,CancellationToken cancellationToken=default);
}

public interface IIntegrationTransport
{
    Task<IntegrationTransportResult> SendAsync(IntegrationTransportRequest request,CancellationToken cancellationToken=default);
}

public sealed record IntegrationTransportRequest(long OutboxId,string Provider,string Destination,string EventType,int ContractVersion,string PayloadJson,string CorrelationId,string IdempotencyKey,string?CredentialReference,string?SigningSecretReference,int TimeoutSeconds);
public sealed record IntegrationTransportResult(bool Success,int?StatusCode,long DurationMilliseconds,string?ErrorCategory,string?SafeError);

public sealed class IntegrationSecurityOptions
{
    public const string SectionName="IntegrationSecurity";public int AllowedClockSkewSeconds{get;set;}=300;public int MaximumInboundBodyBytes{get;set;}=262144;
}
