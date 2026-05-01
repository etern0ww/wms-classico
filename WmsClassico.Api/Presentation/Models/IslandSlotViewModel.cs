namespace WmsClassico.Api.Presentation.Models;

public sealed class IslandSlotViewModel
{
    public required string Code { get; init; }
    public required string SupportedFlow { get; init; }
    public required int CurrentOccupancy { get; init; }
    public required int MaxCapacity { get; init; }
    public required decimal OccupancyPercent { get; init; }
    public required IReadOnlyCollection<string> AllowedDepartureDays { get; init; }
    public bool IsContingency { get; init; }
}
