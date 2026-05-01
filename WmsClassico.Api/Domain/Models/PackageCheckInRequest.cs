namespace WmsClassico.Api.Domain.Models;

public sealed class PackageCheckInRequest
{
    public required string Barcode { get; init; }
    public required string Destination { get; init; }
    public required string FlowType { get; init; }
    public required string RouteCode { get; init; }
    public required DayOfWeek DepartureDay { get; init; }
    public string ServiceType { get; init; } = "PADRAO";
    public DateTime CheckInAt { get; init; } = DateTime.UtcNow;
}
