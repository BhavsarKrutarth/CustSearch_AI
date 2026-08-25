using CustSearch.Application.Operations;
using Microsoft.Extensions.Options;

namespace CustSearch.Infrastructure.Operations;

public sealed class OperationalRetentionOptions
{
    public const string SectionName="OperationalRetention";
    public int IntervalSeconds{get;set;}=300;
    public int BatchSize{get;set;}=100;
    public int RecognitionMetadataRetentionDays{get;set;}=30;
}

public sealed class OperationalRetentionMaintenance(IOperationalPlatformRepository repository,IOptions<OperationalRetentionOptions>options):IOperationalRetentionMaintenance
{
    public Task<RetentionRunResult>RunAsync(CancellationToken ct=default)=>repository.RunRetentionAsync(options.Value.BatchSize,options.Value.RecognitionMetadataRetentionDays,ct);
}
