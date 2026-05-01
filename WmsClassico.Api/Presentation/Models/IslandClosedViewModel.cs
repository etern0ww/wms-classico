namespace WmsClassico.Api.Presentation.Models;

public sealed class IslandClosedViewModel
{
    public required string IslandCode { get; init; }
    public required string IslandName { get; init; }
    public required bool IsClosed { get; init; }
    public required bool IsReadyForDispatch { get; init; }
    public required DateTime? ClosedAt { get; init; }
    public required string FormattedClosedAt { get; init; }
}