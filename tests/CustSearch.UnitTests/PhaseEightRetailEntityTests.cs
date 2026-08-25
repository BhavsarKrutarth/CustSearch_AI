using CustSearch.Domain.Entities;
using CustSearch.Domain.Enums;

namespace CustSearch.UnitTests;

public sealed class PhaseEightRetailEntityTests
{
    private static readonly DateTime Now=DateTime.SpecifyKind(new DateTime(2026,8,23,15,30,0),DateTimeKind.Utc);

    [Fact]
    public void ProductNormalizesSkuAndMoney()
    {
        var product=Product.Create(25," sku-001 "," 890123 ","Silk Saree",null,40,"Brand A","PCS",1299.995m,900.004m,5.12345m,Now);
        Assert.Equal("SKU-001",product.ProductCode);
        Assert.Equal(1300.00m,product.SalePrice);
        Assert.Equal(900.00m,product.CostPrice);
        Assert.Equal(5.1235m,product.TaxPercent);
    }

    [Fact]
    public void InvoiceItemCreatesImmutableServerCalculatedSnapshot()
    {
        var item=RetailInvoiceItem.Create(25,100,500,40,"SKU-1","Silk Saree","Sarees",2m,1000m,100m,5m,Now);
        Assert.Equal("Silk Saree",item.ProductNameSnapshot);
        Assert.Equal(2000m,item.LineSubtotal);
        Assert.Equal(95m,item.TaxAmount);
        Assert.Equal(1995m,item.LineTotal);
    }

    [Fact]
    public void InvoiceRejectsManipulatedGrandTotal()
    {
        var invoice=RetailInvoice.Create(25,10,"INV-1",500,null,null,null,99,Now,null);
        Assert.Throws<ArgumentException>(()=>invoice.SetCalculatedTotals(2000m,100m,95m,2500m,Now));
    }

    [Fact]
    public void InvoiceRejectsOverpayment()
    {
        var invoice=RetailInvoice.Create(25,10,"INV-2",500,null,null,null,99,Now,null);
        invoice.SetCalculatedTotals(2000m,100m,95m,1995m,Now);
        invoice.FinalizeInvoice(Now);
        Assert.Throws<ArgumentOutOfRangeException>(()=>invoice.ApplyPaidAmount(1995.01m,Now));
    }

    [Fact]
    public void PaidInvoiceCannotBeCancelledWithoutRefundOrVoid()
    {
        var invoice=RetailInvoice.Create(25,10,"INV-3",500,null,null,null,99,Now,null);
        invoice.SetCalculatedTotals(1000m,0m,0m,1000m,Now);
        invoice.FinalizeInvoice(Now);
        invoice.ApplyPaidAmount(1000m,Now);
        Assert.Equal(RetailInvoiceStatus.Paid,invoice.Status);
        Assert.Throws<InvalidOperationException>(()=>invoice.Cancel(99,"cancel",Now));
    }
}
