using WmsClassico.Api.Domain.Entities;
using WmsClassico.Api.Domain.Models;

namespace WmsClassico.Api.Infrastructure;

public interface IWarehouseRepository
{
    IReadOnlyCollection<GeographicIsland> GetIslands();
    IReadOnlyCollection<PackageRecord> GetPackages();
    IReadOnlyCollection<PackageStatusSummary> GetPackageStatusSummary();
    GeographicIsland? FindByDestination(string destination);
    PackageRecord? FindPackage(string barcode);
    void RegisterPackage(string islandCode, string slotCode);
    void SavePackage(PackageRecord package);
}
