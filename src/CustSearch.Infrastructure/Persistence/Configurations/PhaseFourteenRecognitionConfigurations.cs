using CustSearch.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustSearch.Infrastructure.Persistence.Configurations;

public sealed class CustomerRecognitionConsentConfiguration:IEntityTypeConfiguration<CustomerRecognitionConsent>
{
    public void Configure(EntityTypeBuilder<CustomerRecognitionConsent>builder){builder.ToTable("CustomerRecognitionConsents","dbo",t=>{t.HasCheckConstraint("CK_RecognitionConsents_Type","[ConsentType]=1");t.HasCheckConstraint("CK_RecognitionConsents_Period","[ExpiresUtc] IS NULL OR [ExpiresUtc]>[GrantedUtc]");t.HasCheckConstraint("CK_RecognitionConsents_Withdrawal","[WithdrawnUtc] IS NULL OR [WithdrawnUtc]>=[GrantedUtc]");});builder.HasKey(x=>x.Id);builder.Property(x=>x.Id).ValueGeneratedOnAdd();builder.Property(x=>x.Purpose).HasMaxLength(200).IsRequired();builder.Property(x=>x.ConsentVersion).HasMaxLength(50).IsRequired();builder.Property(x=>x.EvidenceReference).HasMaxLength(500);builder.Property(x=>x.GrantedUtc).HasPrecision(7);builder.Property(x=>x.ExpiresUtc).HasPrecision(7);builder.Property(x=>x.WithdrawnUtc).HasPrecision(7);builder.Property(x=>x.CreatedUtc).HasPrecision(7);builder.HasIndex(x=>new{x.TenantId,x.CustomerId,x.ConsentType,x.Purpose,x.GrantedUtc});}
}

public sealed class BiometricTemplateConfiguration:IEntityTypeConfiguration<BiometricTemplate>
{
    public void Configure(EntityTypeBuilder<BiometricTemplate>builder){builder.ToTable("BiometricTemplates","dbo",t=>t.HasCheckConstraint("CK_BiometricTemplates_Status","[Status] BETWEEN 1 AND 3"));builder.HasKey(x=>x.Id);builder.Property(x=>x.Id).ValueGeneratedOnAdd();builder.Property(x=>x.EncryptedTemplate).IsRequired();builder.Property(x=>x.Nonce).IsRequired();builder.Property(x=>x.AuthenticationTag).IsRequired();builder.Property(x=>x.EncryptionKeyReference).HasMaxLength(200).IsRequired();builder.Property(x=>x.Algorithm).HasMaxLength(50).IsRequired();builder.Property(x=>x.TemplateVersion).HasMaxLength(50).IsRequired();builder.Property(x=>x.CreatedUtc).HasPrecision(7);builder.Property(x=>x.DisabledUtc).HasPrecision(7);builder.Property(x=>x.DeletedUtc).HasPrecision(7);builder.Property(x=>x.RetentionUntilUtc).HasPrecision(7);builder.HasIndex(x=>new{x.TenantId,x.StoreId,x.CustomerId,x.Status});builder.HasIndex(x=>new{x.TenantId,x.StoreId,x.Id}).IsUnique();}
}

public sealed class RecognitionCandidateConfiguration:IEntityTypeConfiguration<RecognitionCandidate>
{
    public void Configure(EntityTypeBuilder<RecognitionCandidate>builder){builder.ToTable("RecognitionCandidates","dbo",t=>{t.HasCheckConstraint("CK_RecognitionCandidates_Status","[Status] BETWEEN 1 AND 5");t.HasCheckConstraint("CK_RecognitionCandidates_Scores","[Confidence] BETWEEN 0 AND 1 AND [Quality] BETWEEN 0 AND 1 AND ([SecondBestConfidence] IS NULL OR [SecondBestConfidence] BETWEEN 0 AND 1)");});builder.HasKey(x=>x.Id);builder.Property(x=>x.Id).ValueGeneratedOnAdd();builder.Property(x=>x.RequestId).HasMaxLength(150).IsRequired();builder.Property(x=>x.Purpose).HasMaxLength(200).IsRequired();builder.Property(x=>x.Confidence).HasPrecision(5,4);builder.Property(x=>x.Quality).HasPrecision(5,4);builder.Property(x=>x.SecondBestConfidence).HasPrecision(5,4);builder.Property(x=>x.ReviewReason).HasMaxLength(500);builder.Property(x=>x.CreatedUtc).HasPrecision(7);builder.Property(x=>x.ReviewedUtc).HasPrecision(7);builder.HasIndex(x=>new{x.TenantId,x.StoreId,x.RequestId}).IsUnique();builder.HasIndex(x=>new{x.TenantId,x.StoreId,x.Status,x.CreatedUtc});}
}
