using CustSearch.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustSearch.Infrastructure.Persistence.Configurations;

internal sealed class ProductConfiguration:IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> b){b.ToTable("Products","dbo");b.HasKey(x=>x.Id);b.Property(x=>x.Id).ValueGeneratedOnAdd();b.Property(x=>x.ProductCode).HasMaxLength(50).IsRequired();b.Property(x=>x.Barcode).HasMaxLength(100);b.Property(x=>x.Name).HasMaxLength(200).IsRequired();b.Property(x=>x.Description).HasMaxLength(1000);b.Property(x=>x.Brand).HasMaxLength(150);b.Property(x=>x.UnitName).HasMaxLength(50).IsRequired();b.Property(x=>x.SalePrice).HasPrecision(18,2);b.Property(x=>x.CostPrice).HasPrecision(18,2);b.Property(x=>x.TaxPercent).HasPrecision(9,4);b.Property(x=>x.CreatedUtc).HasPrecision(7);b.Property(x=>x.UpdatedUtc).HasPrecision(7);b.HasOne(x=>x.Tenant).WithMany().HasForeignKey(x=>x.TenantId).OnDelete(DeleteBehavior.Restrict);b.HasOne(x=>x.Category).WithMany().HasForeignKey(x=>x.CategoryId).OnDelete(DeleteBehavior.Restrict);b.HasIndex(x=>new{x.TenantId,x.ProductCode}).IsUnique();b.HasIndex(x=>new{x.TenantId,x.Barcode});b.HasIndex(x=>new{x.TenantId,x.CategoryId,x.IsActive});}
}

internal sealed class ProductStoreAvailabilityConfiguration:IEntityTypeConfiguration<ProductStoreAvailability>
{
    public void Configure(EntityTypeBuilder<ProductStoreAvailability> b){b.ToTable("ProductStoreAvailabilities","dbo");b.HasKey(x=>new{x.ProductId,x.StoreId});b.Property(x=>x.CreatedUtc).HasPrecision(7);b.Property(x=>x.UpdatedUtc).HasPrecision(7);b.HasOne(x=>x.Product).WithMany().HasForeignKey(x=>x.ProductId).OnDelete(DeleteBehavior.Cascade);b.HasOne(x=>x.Store).WithMany().HasForeignKey(x=>x.StoreId).OnDelete(DeleteBehavior.Cascade);b.HasIndex(x=>new{x.TenantId,x.StoreId,x.IsActive});}
}

internal sealed class RetailInvoiceConfiguration:IEntityTypeConfiguration<RetailInvoice>
{
    public void Configure(EntityTypeBuilder<RetailInvoice> b){b.ToTable("RetailInvoices","dbo");b.HasKey(x=>x.Id);b.Property(x=>x.Id).ValueGeneratedOnAdd();b.Property(x=>x.InvoiceNumber).HasMaxLength(50).IsRequired();b.Property(x=>x.Subtotal).HasPrecision(18,2);b.Property(x=>x.DiscountAmount).HasPrecision(18,2);b.Property(x=>x.TaxAmount).HasPrecision(18,2);b.Property(x=>x.GrandTotal).HasPrecision(18,2);b.Property(x=>x.PaidAmount).HasPrecision(18,2);b.Property(x=>x.BalanceAmount).HasPrecision(18,2);b.Property(x=>x.Status).HasConversion<byte>();b.Property(x=>x.Notes).HasMaxLength(1000);b.Property(x=>x.CancellationReason).HasMaxLength(500);b.Property(x=>x.InvoiceUtc).HasPrecision(7);b.Property(x=>x.CreatedUtc).HasPrecision(7);b.Property(x=>x.UpdatedUtc).HasPrecision(7);b.Property(x=>x.CancelledUtc).HasPrecision(7);b.Property(x=>x.RowVersion).IsRowVersion();b.HasOne(x=>x.Tenant).WithMany().HasForeignKey(x=>x.TenantId).OnDelete(DeleteBehavior.Restrict);b.HasOne(x=>x.Store).WithMany().HasForeignKey(x=>x.StoreId).OnDelete(DeleteBehavior.Restrict);b.HasOne(x=>x.Customer).WithMany().HasForeignKey(x=>x.CustomerId).OnDelete(DeleteBehavior.Restrict);b.HasOne(x=>x.Household).WithMany().HasForeignKey(x=>x.HouseholdId).OnDelete(DeleteBehavior.Restrict);b.HasOne(x=>x.CustomerVisit).WithMany().HasForeignKey(x=>x.CustomerVisitId).OnDelete(DeleteBehavior.Restrict);b.HasOne(x=>x.VisitParty).WithMany().HasForeignKey(x=>x.VisitPartyId).OnDelete(DeleteBehavior.Restrict);b.HasOne(x=>x.CreatedByUser).WithMany().HasForeignKey(x=>x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);b.HasOne(x=>x.CancelledByUser).WithMany().HasForeignKey(x=>x.CancelledByUserId).OnDelete(DeleteBehavior.Restrict);b.HasIndex(x=>new{x.TenantId,x.StoreId,x.InvoiceNumber}).IsUnique();b.HasIndex(x=>new{x.TenantId,x.StoreId,x.InvoiceUtc});b.HasIndex(x=>new{x.TenantId,x.CustomerId,x.InvoiceUtc});b.HasIndex(x=>new{x.TenantId,x.Status,x.InvoiceUtc});}
}

internal sealed class RetailInvoiceItemConfiguration:IEntityTypeConfiguration<RetailInvoiceItem>
{
    public void Configure(EntityTypeBuilder<RetailInvoiceItem> b){b.ToTable("RetailInvoiceItems","dbo");b.HasKey(x=>x.Id);b.Property(x=>x.Id).ValueGeneratedOnAdd();b.Property(x=>x.ProductCodeSnapshot).HasMaxLength(50).IsRequired();b.Property(x=>x.ProductNameSnapshot).HasMaxLength(200).IsRequired();b.Property(x=>x.CategoryNameSnapshot).HasMaxLength(150);b.Property(x=>x.Quantity).HasPrecision(18,4);b.Property(x=>x.UnitPrice).HasPrecision(18,2);b.Property(x=>x.DiscountAmount).HasPrecision(18,2);b.Property(x=>x.TaxPercent).HasPrecision(9,4);b.Property(x=>x.TaxAmount).HasPrecision(18,2);b.Property(x=>x.LineSubtotal).HasPrecision(18,2);b.Property(x=>x.LineTotal).HasPrecision(18,2);b.Property(x=>x.CreatedUtc).HasPrecision(7);b.HasOne(x=>x.Invoice).WithMany().HasForeignKey(x=>x.InvoiceId).OnDelete(DeleteBehavior.Cascade);b.HasOne(x=>x.Product).WithMany().HasForeignKey(x=>x.ProductId).OnDelete(DeleteBehavior.Restrict);b.HasOne(x=>x.Category).WithMany().HasForeignKey(x=>x.CategoryId).OnDelete(DeleteBehavior.Restrict);b.HasIndex(x=>new{x.TenantId,x.InvoiceId});b.HasIndex(x=>new{x.TenantId,x.ProductId});b.HasIndex(x=>new{x.TenantId,x.CategoryId});}
}

internal sealed class RetailInvoicePaymentConfiguration:IEntityTypeConfiguration<RetailInvoicePayment>
{
    public void Configure(EntityTypeBuilder<RetailInvoicePayment> b){b.ToTable("RetailInvoicePayments","dbo");b.HasKey(x=>x.Id);b.Property(x=>x.Id).ValueGeneratedOnAdd();b.Property(x=>x.PaymentReference).HasMaxLength(100).IsRequired();b.Property(x=>x.PaymentMethod).HasConversion<byte>();b.Property(x=>x.Amount).HasPrecision(18,2);b.Property(x=>x.PaymentUtc).HasPrecision(7);b.Property(x=>x.Status).HasConversion<byte>();b.Property(x=>x.ExternalTransactionId).HasMaxLength(150);b.Property(x=>x.Notes).HasMaxLength(500);b.Property(x=>x.CreatedUtc).HasPrecision(7);b.HasOne(x=>x.Invoice).WithMany().HasForeignKey(x=>x.InvoiceId).OnDelete(DeleteBehavior.Restrict);b.HasOne(x=>x.ReceivedByUser).WithMany().HasForeignKey(x=>x.ReceivedByUserId).OnDelete(DeleteBehavior.Restrict);b.HasIndex(x=>new{x.TenantId,x.PaymentReference}).IsUnique();b.HasIndex(x=>new{x.TenantId,x.StoreId,x.PaymentUtc});b.HasIndex(x=>new{x.TenantId,x.InvoiceId,x.Status});}
}

internal sealed class RetailInvoiceParticipantConfiguration:IEntityTypeConfiguration<RetailInvoiceParticipant>
{
    public void Configure(EntityTypeBuilder<RetailInvoiceParticipant> b){b.ToTable("RetailInvoiceParticipants","dbo");b.HasKey(x=>new{x.InvoiceId,x.CustomerId});b.Property(x=>x.ParticipationType).HasConversion<byte>();b.Property(x=>x.CreatedUtc).HasPrecision(7);b.HasOne(x=>x.Invoice).WithMany().HasForeignKey(x=>x.InvoiceId).OnDelete(DeleteBehavior.Cascade);b.HasOne(x=>x.Customer).WithMany().HasForeignKey(x=>x.CustomerId).OnDelete(DeleteBehavior.Restrict);b.HasIndex(x=>new{x.TenantId,x.CustomerId});b.HasIndex(x=>new{x.TenantId,x.InvoiceId,x.IsPayer});}
}

internal sealed class RetailInvoiceItemAttributionConfiguration:IEntityTypeConfiguration<RetailInvoiceItemAttribution>
{
    public void Configure(EntityTypeBuilder<RetailInvoiceItemAttribution> b){b.ToTable("RetailInvoiceItemAttributions","dbo");b.HasKey(x=>x.Id);b.Property(x=>x.Id).ValueGeneratedOnAdd();b.Property(x=>x.AttributionType).HasConversion<byte>();b.Property(x=>x.QuantityAttributed).HasPrecision(18,4);b.Property(x=>x.AmountAttributed).HasPrecision(18,2);b.Property(x=>x.Source).HasConversion<byte>();b.Property(x=>x.CreatedUtc).HasPrecision(7);b.HasOne(x=>x.Invoice).WithMany().HasForeignKey(x=>x.InvoiceId).OnDelete(DeleteBehavior.Cascade);b.HasOne(x=>x.InvoiceItem).WithMany().HasForeignKey(x=>x.InvoiceItemId).OnDelete(DeleteBehavior.Cascade);b.HasOne(x=>x.Customer).WithMany().HasForeignKey(x=>x.CustomerId).OnDelete(DeleteBehavior.Restrict);b.HasOne(x=>x.CreatedByUser).WithMany().HasForeignKey(x=>x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);b.HasIndex(x=>new{x.TenantId,x.InvoiceItemId,x.CustomerId}).IsUnique();b.HasIndex(x=>new{x.TenantId,x.CustomerId});}
}
