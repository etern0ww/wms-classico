namespace WmsClassico.Api.Domain.Entities;

public sealed class IslandSlot
{
    public required string Code { get; init; }
    public required string SupportedFlow { get; init; }
    public required IReadOnlyCollection<DayOfWeek> AllowedDepartureDays { get; init; }
    public required int MaxCapacity { get; init; }
    public required int CurrentOccupancy { get; set; }
    public bool IsContingency { get; init; }

    public decimal OccupancyPercent => MaxCapacity == 0
        ? 0
        : Math.Round((decimal)CurrentOccupancy / MaxCapacity * 100, 2);

    public bool SupportsDay(DayOfWeek dayOfWeek)
    {
        return AllowedDepartureDays.Count == 0 || AllowedDepartureDays.Contains(dayOfWeek);
    }

    public bool SupportsFlow(string flowType)
    {
        return SupportedFlow.Equals("AMBOS", StringComparison.OrdinalIgnoreCase) ||
               SupportedFlow.Equals(flowType, StringComparison.OrdinalIgnoreCase);
    }

    public void RegisterPackage()
    {
        CurrentOccupancy++;
    }
}
