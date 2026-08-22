using CustSearch.API.Middleware;

namespace CustSearch.IntegrationTests;

public sealed class CorrelationIdPolicyTests
{
    [Theory]
    [InlineData("request-123")]
    [InlineData("client_123.trace")]
    public void IsValidAcceptsLogSafeIdentifiers(string value) =>
        Assert.True(CorrelationIdMiddleware.IsValid(value));

    [Theory]
    [InlineData("")]
    [InlineData("contains spaces")]
    [InlineData("contains\r\nnewline")]
    public void IsValidRejectsUnsafeIdentifiers(string value) =>
        Assert.False(CorrelationIdMiddleware.IsValid(value));
}
