namespace WmsClassico.Api.Domain.Models;

public sealed class PackageStatusSummary
{
    public required string StatusCode { get; init; }
    public required string StatusLabel { get; init; }
    public required int Quantity { get; init; }
}
