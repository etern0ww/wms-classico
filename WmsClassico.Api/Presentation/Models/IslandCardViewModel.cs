namespace WmsClassico.Api.Presentation.Models;

public sealed class IslandCardViewModel
{
    public required string Code { get; init; }
    public required string Name { get; init; }
    public required string DestinationKey { get; init; }
    public required int DistanceRank { get; init; }
    public required int CurrentOccupancy { get; init; }
    public required int MaxCapacity { get; init; }
    public required decimal OccupancyPercent { get; init; }
    public required string FlowStatusCode { get; init; }
    public required decimal XptSharePercent { get; init; }
    public IReadOnlyCollection<IslandSlotViewModel> Slots { get; init; } = [];
}
