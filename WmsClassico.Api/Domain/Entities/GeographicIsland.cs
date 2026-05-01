namespace WmsClassico.Api.Domain.Entities;

public sealed class GeographicIsland
{
    public required string Id { get; init; }
    public required string Code { get; init; }
    public required string Name { get; init; }
    public required string DestinationKey { get; init; }
    public int DistanceRank { get; init; }
    public int MaxCapacity { get; init; }
    public int CurrentOccupancy { get; set; }
    public List<IslandSlot> Slots { get; init; } = [];

    public decimal OccupancyPercent => MaxCapacity == 0
        ? 0
        : Math.Round((decimal)CurrentOccupancy / MaxCapacity * 100, 2);

    public string FlowStatusCode => OccupancyPercent switch
    {
        < 80m => "VERDE",
        <= 90m => "AMARELO",
        _ => "VERMELHO"
    };

    public int XptOccupancy => Slots
        .Where(slot => slot.SupportedFlow == "XPT")
        .Sum(slot => slot.CurrentOccupancy);

    public decimal XptSharePercent => CurrentOccupancy == 0
        ? 0
        : Math.Round((decimal)XptOccupancy / CurrentOccupancy * 100, 2);

    public void RegisterPackage(string slotCode)
    {
        var slot = Slots.First(slot => slot.Code.Equals(slotCode, StringComparison.OrdinalIgnoreCase));
        slot.RegisterPackage();
        CurrentOccupancy++;
    }
}
