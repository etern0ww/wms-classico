using WmsClassico.Api.Domain.Models;

namespace WmsClassico.Api.Domain.Entities;

public sealed class PackageRecord
{
    public string Barcode { get; init; }
    public string Destination { get; set; }
    public string RouteCode { get; set; }
    public string FlowType { get; set; }
    public PackageOperationalStatus Status { get; private set; }
    public PackageExceptionReason ExceptionReason { get; private set; }
    public string? BrotherBarcode { get; private set; }
    public DateOnly? PlannedDepartureDate { get; private set; }
    public string? LastIslandCode { get; private set; }
    public string? LastSlotCode { get; private set; }
    public DateTime LastUpdatedAt { get; private set; }
    public List<PackageTrackingEvent> Events { get; } = [];

    public PackageRecord(
        string barcode,
        string destination,
        string routeCode,
        string flowType,
        PackageOperationalStatus status,
        PackageExceptionReason exceptionReason,
        DateTime lastUpdatedAt)
    {
        Barcode = barcode;
        Destination = destination;
        RouteCode = routeCode;
        FlowType = flowType;
        Status = status;
        ExceptionReason = exceptionReason;
        LastUpdatedAt = lastUpdatedAt;
    }

    public void UpdateOperationalState(
        PackageOperationalStatus status,
        PackageExceptionReason exceptionReason,
        DateTime timestamp,
        string description,
        string? islandCode = null,
        string? slotCode = null,
        DateOnly? plannedDepartureDate = null,
        string? brotherBarcode = null)
    {
        Status = status;
        ExceptionReason = exceptionReason;
        LastIslandCode = islandCode ?? LastIslandCode;
        LastSlotCode = slotCode ?? LastSlotCode;
        PlannedDepartureDate = plannedDepartureDate ?? PlannedDepartureDate;
        BrotherBarcode = brotherBarcode ?? BrotherBarcode;
        LastUpdatedAt = timestamp;

        Events.Add(new PackageTrackingEvent
        {
            Timestamp = timestamp,
            Status = status,
            ExceptionReason = exceptionReason,
            Description = description,
            IslandCode = islandCode,
            SlotCode = slotCode
        });
    }
}
