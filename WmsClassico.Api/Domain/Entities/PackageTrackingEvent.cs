using WmsClassico.Api.Domain.Models;

namespace WmsClassico.Api.Domain.Entities;

public sealed class PackageTrackingEvent
{
    public required DateTime Timestamp { get; init; }
    public required PackageOperationalStatus Status { get; init; }
    public required PackageExceptionReason ExceptionReason { get; init; }
    public required string Description { get; init; }
    public string? IslandCode { get; init; }
    public string? SlotCode { get; init; }
}
