namespace CustSearch.Application.PlatformTenancy;

/// <summary>Signals that requested platform data does not exist without exposing database details.</summary>
public sealed class PlatformResourceNotFoundException(string resource) : Exception($"{resource} was not found.");

/// <summary>Signals that a platform management request violates a safe business rule.</summary>
public sealed class PlatformBusinessRuleException(string message) : Exception(message);

/// <summary>Signals that another writer changed the resource after the caller loaded it.</summary>
public sealed class PlatformConcurrencyException() : Exception("The resource changed. Reload it and retry.");
