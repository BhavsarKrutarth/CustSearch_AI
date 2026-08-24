using System.Text.RegularExpressions;
using CustSearch.Application.Integrations;
using Microsoft.Extensions.Configuration;

namespace CustSearch.Integrations;

/// <summary>Resolves opaque identifiers from environment/vault-backed configuration; no value is persisted or returned by APIs.</summary>
public sealed partial class ConfigurationIntegrationSecretResolver(IConfiguration configuration):IIntegrationSecretResolver
{
    public ValueTask<string?>ResolveAsync(string reference,CancellationToken cancellationToken=default)
    {cancellationToken.ThrowIfCancellationRequested();ArgumentException.ThrowIfNullOrWhiteSpace(reference);if(!ReferencePattern().IsMatch(reference))return ValueTask.FromResult<string?>(null);return ValueTask.FromResult(configuration[$"IntegrationSecrets:{reference}"]);}
    [GeneratedRegex("^[A-Za-z0-9_.-]{1,200}$",RegexOptions.CultureInvariant)]private static partial Regex ReferencePattern();
}
