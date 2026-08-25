using CustSearch.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustSearch.Infrastructure.Persistence.Configurations;

internal sealed class PlatformInvoiceConfiguration:IEntityTypeConfiguration<PlatformInvoice>
{
    public void Configure(EntityTypeBuilder<PlatformInvoice> b)
    {
        b.ToTable("PlatformInvoices","dbo",t=>{t.HasCheckConstraint("CK_PlatformInvoices_Status","[Status] BETWEEN 1 AND 5");t.HasCheckConstraint("CK_PlatformInvoices_Amounts","[Subtotal]>=0 AND [DiscountAmount]>=0 AND [TaxAmount]>=0 AND [Total]>=0 AND [PaidAmount]>=0 AND [PaidAmount]<=[Total]");});
        b.HasKey(x=>x.Id);b.Property(x=>x.Id).ValueGeneratedOnAdd();b.Property(x=>x.InvoiceNumber).HasMaxLength(60).IsRequired();b.Property(x=>x.Currency).HasMaxLength(3).IsFixedLength().IsRequired();b.Property(x=>x.Status).HasConversion<byte>();b.Property(x=>x.Subtotal).HasPrecision(19,4);b.Property(x=>x.DiscountAmount).HasPrecision(19,4);b.Property(x=>x.TaxAmount).HasPrecision(19,4);b.Property(x=>x.Total).HasPrecision(19,4);b.Property(x=>x.PaidAmount).HasPrecision(19,4);b.Property(x=>x.InvoiceUtc).HasPrecision(7);b.Property(x=>x.DueUtc).HasPrecision(7);b.Property(x=>x.CreatedUtc).HasPrecision(7);b.Property(x=>x.UpdatedUtc).HasPrecision(7).IsConcurrencyToken();b.Property(x=>x.RowVersion).HasMaxLength(16).IsFixedLength().IsConcurrencyToken();
        b.HasOne(x=>x.Tenant).WithMany().HasForeignKey(x=>x.TenantId).OnDelete(DeleteBehavior.Restrict);b.HasOne(x=>x.TenantSubscription).WithMany().HasForeignKey(x=>x.TenantSubscriptionId).OnDelete(DeleteBehavior.Restrict);b.HasIndex(x=>new{x.TenantId,x.InvoiceNumber}).IsUnique();b.HasIndex(x=>new{x.TenantId,x.Status,x.InvoiceUtc});
    }
}

internal sealed class PlatformInvoiceItemConfiguration:IEntityTypeConfiguration<PlatformInvoiceItem>
{
    public void Configure(EntityTypeBuilder<PlatformInvoiceItem> b)
    {
        b.ToTable("PlatformInvoiceItems","dbo",t=>t.HasCheckConstraint("CK_PlatformInvoiceItems_Amounts","[Quantity]>0 AND [Rate]>=0 AND [DiscountAmount]>=0 AND [TaxAmount]>=0 AND [Subtotal]>=0 AND [Total]>=0"));b.HasKey(x=>x.Id);b.Property(x=>x.Id).ValueGeneratedOnAdd();b.Property(x=>x.PlanName).HasMaxLength(150).IsRequired();b.Property(x=>x.Description).HasMaxLength(500);b.Property(x=>x.Quantity).HasPrecision(19,4);b.Property(x=>x.Rate).HasPrecision(19,4);b.Property(x=>x.DiscountAmount).HasPrecision(19,4);b.Property(x=>x.TaxAmount).HasPrecision(19,4);b.Property(x=>x.Subtotal).HasPrecision(19,4);b.Property(x=>x.Total).HasPrecision(19,4);b.Property(x=>x.CreatedUtc).HasPrecision(7);b.HasOne(x=>x.PlatformInvoice).WithMany().HasForeignKey(x=>x.PlatformInvoiceId).OnDelete(DeleteBehavior.Cascade);b.HasIndex(x=>new{x.TenantId,x.PlatformInvoiceId});
    }
}

internal sealed class PlatformPaymentConfiguration:IEntityTypeConfiguration<PlatformPayment>
{
    public void Configure(EntityTypeBuilder<PlatformPayment> b)
    {
        b.ToTable("PlatformPayments","dbo",t=>{t.HasCheckConstraint("CK_PlatformPayments_Status","[Status] BETWEEN 1 AND 4");t.HasCheckConstraint("CK_PlatformPayments_Amount","[Amount]>0");});b.HasKey(x=>x.Id);b.Property(x=>x.Id).ValueGeneratedOnAdd();b.Property(x=>x.PaymentMethod).HasMaxLength(50).IsRequired();b.Property(x=>x.Amount).HasPrecision(19,4);b.Property(x=>x.Currency).HasMaxLength(3).IsFixedLength().IsRequired();b.Property(x=>x.GatewayReference).HasMaxLength(150);b.Property(x=>x.TransactionReference).HasMaxLength(150).IsRequired();b.Property(x=>x.Status).HasConversion<byte>();b.Property(x=>x.PaymentUtc).HasPrecision(7);b.Property(x=>x.CreatedUtc).HasPrecision(7);b.Property(x=>x.UpdatedUtc).HasPrecision(7);b.HasOne(x=>x.Tenant).WithMany().HasForeignKey(x=>x.TenantId).OnDelete(DeleteBehavior.Restrict);b.HasOne(x=>x.PlatformInvoice).WithMany().HasForeignKey(x=>x.PlatformInvoiceId).OnDelete(DeleteBehavior.Restrict);b.HasIndex(x=>new{x.TenantId,x.TransactionReference}).IsUnique();b.HasIndex(x=>new{x.TenantId,x.PlatformInvoiceId,x.Status});
    }
}
