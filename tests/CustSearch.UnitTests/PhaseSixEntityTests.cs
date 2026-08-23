using CustSearch.Domain.Entities;

namespace CustSearch.UnitTests;

public sealed class PhaseSixEntityTests
{
    [Fact]
    public void CustomerNormalizesCodeEmailAndOptionalFields()
    {
        var now = DateTime.SpecifyKind(new DateTime(2026, 8, 23, 9, 0, 0), DateTimeKind.Utc);
        var customer = Customer.Create(25, " cust-001 ", " Priya ", " Shah ", " 9876543210 ", " PRIYA@EXAMPLE.COM ", " likes sarees ", now);

        Assert.Equal("CUST-001", customer.CustomerCode);
        Assert.Equal("Priya", customer.FirstName);
        Assert.Equal("Shah", customer.LastName);
        Assert.Equal("priya@example.com", customer.Email);
        Assert.True(customer.IsActive);
        Assert.Equal(now, customer.CreatedUtc);
    }

    [Fact]
    public void AnonymousVisitorRemainsAnonymousUntilExplicitConversion()
    {
        var first = DateTime.SpecifyKind(new DateTime(2026, 8, 23, 9, 0, 0), DateTimeKind.Utc);
        var visitor = AnonymousVisitor.Create(25, 10, "vis-001", first);

        Assert.Null(visitor.ConvertedCustomerId);
        Assert.True(visitor.IsActive);

        visitor.ConvertToCustomer(500, first.AddMinutes(12));
        Assert.Equal(500, visitor.ConvertedCustomerId);
        Assert.False(visitor.IsActive);
        Assert.NotNull(visitor.ConvertedUtc);
    }

    [Fact]
    public void AnonymousVisitorCannotBeConvertedTwice()
    {
        var first = DateTime.SpecifyKind(new DateTime(2026, 8, 23, 9, 0, 0), DateTimeKind.Utc);
        var visitor = AnonymousVisitor.Create(25, 10, "VIS-002", first);
        visitor.ConvertToCustomer(500, first.AddMinutes(5));

        Assert.Throws<InvalidOperationException>(() => visitor.ConvertToCustomer(501, first.AddMinutes(6)));
    }
}
