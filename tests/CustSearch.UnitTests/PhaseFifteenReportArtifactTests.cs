using System.IO.Compression;
using System.Text;
using CustSearch.Application.ReportsExports;
using CustSearch.Infrastructure.ReportsExports;
using Microsoft.Extensions.Options;

namespace CustSearch.UnitTests;

public sealed class PhaseFifteenReportArtifactTests:IDisposable
{
    private readonly string root=Path.Combine(Path.GetTempPath(),"custsearch-report-tests",Guid.NewGuid().ToString("N"));
    [Theory]
    [InlineData(ReportExportFormat.Csv,"csv")]
    [InlineData(ReportExportFormat.Excel,"xlsx")]
    [InlineData(ReportExportFormat.Pdf,"pdf")]
    public async Task WritersCreateValidPrivateArtifacts(ReportExportFormat format,string extension)
    {
        var store=Create();var data=new ReportDataView(["Name","Amount"],[new Dictionary<string,object?>{{"Name","=danger"},{"Amount",12.5m}}]);var artifact=await store.WriteAsync(42,format,data);
        Assert.EndsWith($".{extension}",artifact.StorageReference,StringComparison.Ordinal);Assert.DoesNotContain("/",artifact.StorageReference,StringComparison.Ordinal);Assert.DoesNotContain("\\",artifact.StorageReference,StringComparison.Ordinal);Assert.Equal(64,artifact.Sha256.Length);Assert.True(artifact.ContentLength>0);
        await using var stream=await store.OpenReadAsync(artifact.StorageReference);var prefix=new byte[8];var read=await stream.ReadAsync(prefix);Assert.True(read>=2);
        if(format==ReportExportFormat.Excel){Assert.Equal((byte)'P',prefix[0]);Assert.Equal((byte)'K',prefix[1]);stream.Position=0;using var zip=new ZipArchive(stream,ZipArchiveMode.Read,true);Assert.NotNull(zip.GetEntry("xl/worksheets/sheet1.xml"));}
        if(format==ReportExportFormat.Pdf)Assert.Equal("%PDF-",Encoding.ASCII.GetString(prefix,0,5));
        if(format==ReportExportFormat.Csv){stream.Position=0;using var reader=new StreamReader(stream,Encoding.UTF8,true,leaveOpen:true);var text=await reader.ReadToEndAsync();Assert.Contains("'=danger",text,StringComparison.Ordinal);}
    }

    [Fact]
    public async Task ArtifactReaderRejectsPathTraversal()
    {
        await Assert.ThrowsAsync<ReportExportNotFoundException>(()=>Create().OpenReadAsync("../secret.txt"));
    }

    [Fact]
    public async Task RetentionDeleteIsIdempotentAndRejectsPathTraversal()
    {
        var store=Create();var artifact=await store.WriteAsync(43,ReportExportFormat.Csv,new(["Id"],[new Dictionary<string,object?>{{"Id",43}}]));
        await store.DeleteAsync(artifact.StorageReference);await store.DeleteAsync(artifact.StorageReference);
        await Assert.ThrowsAsync<ReportExportNotFoundException>(()=>store.OpenReadAsync(artifact.StorageReference));
        await Assert.ThrowsAsync<ReportExportNotFoundException>(()=>store.DeleteAsync("../outside.csv"));
    }

    private ReportArtifactStore Create()=>new(Options.Create(new ReportExportOptions{StorageRoot=root}));
    public void Dispose(){var full=Path.GetFullPath(root);var expected=Path.GetFullPath(Path.Combine(Path.GetTempPath(),"custsearch-report-tests"))+Path.DirectorySeparatorChar;if(Directory.Exists(full)&&full.StartsWith(expected,StringComparison.OrdinalIgnoreCase))Directory.Delete(full,true);}
}
