namespace WmsClassico.Api.Presentation.Models;

public sealed class PackageStatusSummaryViewModel
{
    public required string StatusCode { get; init; }
    public required string StatusLabel { get; init; }
    public required int Quantity { get; init; }
}
