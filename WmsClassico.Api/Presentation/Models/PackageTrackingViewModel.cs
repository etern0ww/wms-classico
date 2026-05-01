namespace WmsClassico.Api.Presentation.Models;

public sealed class PackageTrackingViewModel
{
    public required string Barcode { get; init; }
    public required string Destination { get; init; }
    public required string RouteCode { get; init; }
    public required string FlowType { get; init; }
    public required string StatusLabel { get; init; }
    public required string ExceptionReasonLabel { get; init; }
    public string? LastIslandCode { get; init; }
    public string? LastSlotCode { get; init; }
    public string? BrotherBarcode { get; init; }
    public string? PlannedDepartureDate { get; init; }
    public required string LastUpdatedAt { get; init; }
    public bool IsProblemCase { get; init; }
}
