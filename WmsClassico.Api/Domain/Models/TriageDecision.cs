namespace WmsClassico.Api.Domain.Models;

public sealed class TriageDecision
{
    public required string Barcode { get; init; }
    public required string Destination { get; init; }
    public required string FlowType { get; init; }
    public required string RouteCode { get; init; }
    public required string IslandCode { get; init; }
    public required string IslandName { get; init; }
    public required string SlotCode { get; init; }
    public required string FlowStatus { get; init; }
    public required bool AllowDirection { get; init; }
    public required int PriorityScore { get; init; }
    public required string Action { get; init; }
    public required string Reason { get; init; }
    public PackageOperationalStatus PackageStatus { get; init; }
    public PackageExceptionReason ExceptionReason { get; init; }
    public IReadOnlyCollection<string> Alerts { get; init; } = [];
    public IReadOnlyCollection<PokaYokeAlert> PokaYokeAlerts { get; init; } = [];
}
