namespace WmsClassico.Api.Presentation.Models;

public sealed class CageViewModel
{
    public required string Code { get; init; }
    public required string Name { get; init; }
    public required int CurrentOccupancy { get; init; }
    public required int MaxCapacity { get; init; }
    public required decimal OccupancyPercent { get; init; }
    public required int WaveNumber { get; init; }
    public required bool IsReadyForDispatch { get; init; }
}