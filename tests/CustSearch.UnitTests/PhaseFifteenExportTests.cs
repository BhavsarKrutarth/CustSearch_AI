using CustSearch.Application.ReportsExports;
using CustSearch.Domain.Entities;
using CustSearch.Domain.Enums;
using CustSearch.Infrastructure.ReportsExports;
using Microsoft.Extensions.Options;

namespace CustSearch.UnitTests;

public sealed class PhaseFifteenExportTests
{
    private static readonly DateTime Now=new(2026,8,25,12,0,0,DateTimeKind.Utc);
    [Fact]public void ExportLifecycleRequiresLeaseAndSupportsBoundedRetry(){var job=ExportJob.Queue(7,9,ReportType.Customers,ExportFormat.Csv,"{}","[3]",Now,Now.AddDays(1));var lease=Guid.NewGuid();job.Claim(lease,Now.AddMinutes(1),Now.AddMinutes(3));job.ReportProgress(50,lease);Assert.Throws<InvalidOperationException>(()=>job.Complete("safe.csv","safe.csv","text/csv",Guid.NewGuid(),Now.AddMinutes(2)));job.Fail("safe failure",lease,Now.AddMinutes(2));job.Retry(Now.AddMinutes(3));Assert.Equal(ExportJobStatus.Queued,job.Status);Assert.Equal(1,job.AttemptCount);}
    [Fact]public void ExpiredExportClearsFileMetadata(){var job=ExportJob.Queue(7,9,ReportType.Customers,ExportFormat.Csv,"{}","[3]",Now,Now.AddHours(1));var lease=Guid.NewGuid();job.Claim(lease,Now.AddMinutes(1),Now.AddMinutes(3));job.Complete("/safe/report.csv","report.csv","text/csv",lease,Now.AddMinutes(2));job.Expire(Now.AddHours(1));Assert.Equal(ExportJobStatus.Expired,job.Status);Assert.Null(job.FilePath);Assert.False(job.Progress>0);}
    [Fact]public void ReportFiltersAreBoundedAndInjectionSafe(){var filter=new ReportFilter(Now.AddDays(-1),Now,[2,2,1],1,500).Normalize();Assert.Equal(new long[]{1,2},filter.StoreIds);Assert.Throws<ReportExportException>(()=>new ReportFilter(Now.AddDays(-400),Now,[],1,100).Normalize());Assert.Throws<ReportExportException>(()=>new ReportFilter(Now.AddDays(-1),Now,[],1,501).Normalize());}
    [Theory][InlineData(ExportFormat.Csv,"text/csv",new byte[]{0xEF,0xBB,0xBF})][InlineData(ExportFormat.Excel,"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",new byte[]{0x50,0x4B})][InlineData(ExportFormat.Pdf,"application/pdf",new byte[]{0x25,0x50,0x44,0x46})]public async Task CsvExcelAndPdfHaveCorrectSignatures(ExportFormat format,string contentType,byte[]signature){var root=Path.Combine(Path.GetTempPath(),$"p15-format-{Guid.NewGuid():N}");try{var store=new LocalExportFileStore(Options.Create(new ReportsExportsOptions{StoragePath=root}));var report=new ReportResultView(ReportType.Customers,Now.AddDays(-1),Now,1,100,1,[new("Customers",4,"Count",1,"safe",Now)]);var saved=await store.SaveAsync(1,format,report);Assert.Equal(contentType,saved.ContentType);await using var stream=await store.OpenReadAsync(saved.Path);var actual=new byte[signature.Length];Assert.Equal(signature.Length,await stream.ReadAsync(actual));Assert.Equal(signature,actual);await store.DeleteAsync(saved.Path);Assert.False(File.Exists(saved.Path));}finally{if(Directory.Exists(root))Directory.Delete(root,true);}}
    [Fact]public void DownloadTokenIsUserTenantAndExpiryBound(){var service=new ExportDownloadTokenService(Options.Create(new ReportsExportsOptions{DownloadSigningKey=new string('k',32)}));var ticket=service.Create(3,7,9,Now.AddMinutes(5));service.Validate(ticket.Token,3,7,9,Now);Assert.Throws<ReportExportException>(()=>service.Validate(ticket.Token,3,8,9,Now));Assert.Throws<ReportExportException>(()=>service.Validate(ticket.Token,3,7,9,Now.AddMinutes(6)));}
}
