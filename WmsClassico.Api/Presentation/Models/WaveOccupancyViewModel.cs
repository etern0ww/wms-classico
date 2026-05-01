namespace WmsClassico.Api.Presentation.Models;

public sealed class WaveOccupancyViewModel
{
    public required int WaveNumber { get; init; }
    public required string WaveName { get; init; }
    public required IReadOnlyCollection<string> Islands { get; init; }
    public required int TotalCapacity { get; init; }
    public required int CurrentOccupancy { get; init; }
    public required decimal OccupancyPercent { get; init; }
}