using WmsClassico.Api.Domain.Entities;
using WmsClassico.Api.Domain.Models;

namespace WmsClassico.Api.Infrastructure;

public sealed class InMemoryWarehouseRepository : IWarehouseRepository
{
    private readonly List<GeographicIsland> _islands =
    [
        new GeographicIsland
        {
            Id = "1",
            Code = "GEO_01",
            Name = "Timon",
            DestinationKey = "TIMON",
            DistanceRank = 1,
            MaxCapacity = 500,
            CurrentOccupancy = 260,
            Slots =
            [
                new IslandSlot { Code = "A1", SupportedFlow = "CAPITAL", AllowedDepartureDays = [], MaxCapacity = 150, CurrentOccupancy = 85 },
                new IslandSlot { Code = "X1", SupportedFlow = "XPT", AllowedDepartureDays = [], MaxCapacity = 150, CurrentOccupancy = 80 },
                new IslandSlot { Code = "A5", SupportedFlow = "AMBOS", AllowedDepartureDays = [], MaxCapacity = 200, CurrentOccupancy = 95 }
            ]
        },
        new GeographicIsland
        {
            Id = "2",
            Code = "GEO_02",
            Name = "Lagoa Alegre",
            DestinationKey = "LAGOA ALEGRE",
            DistanceRank = 2,
            MaxCapacity = 450,
            CurrentOccupancy = 255,
            Slots =
            [
                new IslandSlot { Code = "A3", SupportedFlow = "CAPITAL", AllowedDepartureDays = [DayOfWeek.Monday, DayOfWeek.Tuesday], MaxCapacity = 140, CurrentOccupancy = 68 },
                new IslandSlot { Code = "A4", SupportedFlow = "CAPITAL", AllowedDepartureDays = [DayOfWeek.Wednesday, DayOfWeek.Thursday], MaxCapacity = 140, CurrentOccupancy = 71 },
                new IslandSlot { Code = "X1", SupportedFlow = "XPT", AllowedDepartureDays = [], MaxCapacity = 170, CurrentOccupancy = 116 }
            ]
        },
        new GeographicIsland
        {
            Id = "3",
            Code = "GEO_03",
            Name = "Monte Castelo",
            DestinationKey = "MONTE CASTELO",
            DistanceRank = 3,
            MaxCapacity = 420,
            CurrentOccupancy = 296,
            Slots =
            [
                new IslandSlot { Code = "A6", SupportedFlow = "CAPITAL", AllowedDepartureDays = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday], MaxCapacity = 150, CurrentOccupancy = 103 },
                new IslandSlot { Code = "A7", SupportedFlow = "CAPITAL", AllowedDepartureDays = [DayOfWeek.Thursday, DayOfWeek.Friday], MaxCapacity = 120, CurrentOccupancy = 88 },
                new IslandSlot { Code = "X3", SupportedFlow = "XPT", AllowedDepartureDays = [], MaxCapacity = 150, CurrentOccupancy = 105 }
            ]
        },
        new GeographicIsland
        {
            Id = "4",
            Code = "GEO_05",
            Name = "Jose de Freitas",
            DestinationKey = "JOSE DE FREITAS",
            DistanceRank = 5,
            MaxCapacity = 520,
            CurrentOccupancy = 346,
            Slots =
            [
                new IslandSlot
                {
                    Code = "A3",
                    SupportedFlow = "CAPITAL",
                    AllowedDepartureDays = [DayOfWeek.Monday, DayOfWeek.Tuesday],
                    MaxCapacity = 110,
                    CurrentOccupancy = 62
                },
                new IslandSlot
                {
                    Code = "A4",
                    SupportedFlow = "CAPITAL",
                    AllowedDepartureDays = [DayOfWeek.Wednesday, DayOfWeek.Thursday],
                    MaxCapacity = 110,
                    CurrentOccupancy = 58
                },
                new IslandSlot
                {
                    Code = "A9",
                    SupportedFlow = "CAPITAL",
                    AllowedDepartureDays = [DayOfWeek.Friday, DayOfWeek.Saturday],
                    MaxCapacity = 120,
                    CurrentOccupancy = 77
                },
                new IslandSlot
                {
                    Code = "X1",
                    SupportedFlow = "XPT",
                    AllowedDepartureDays = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday],
                    MaxCapacity = 90,
                    CurrentOccupancy = 78
                },
                new IslandSlot
                {
                    Code = "X2",
                    SupportedFlow = "XPT",
                    AllowedDepartureDays = [DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday],
                    MaxCapacity = 70,
                    CurrentOccupancy = 66
                },
                new IslandSlot
                {
                    Code = "PULMAO_XPT",
                    SupportedFlow = "XPT",
                    AllowedDepartureDays = [],
                    MaxCapacity = 20,
                    CurrentOccupancy = 5,
                    IsContingency = true
                }
            ]
        }
    ];

    private readonly List<PackageRecord> _packages =
    [
        BuildPackage("PKG-0001", "Jose de Freitas", "PM-A", "CAPITAL", PackageOperationalStatus.Para_Expedir, PackageExceptionReason.Nenhum, "GEO_05", "A3", "Recebido e aguardando composicao de rota."),
        BuildPackage("PKG-0002", "Jose de Freitas", "AM5-J", "XPT", PackageOperationalStatus.Em_Rota, PackageExceptionReason.Nenhum, "GEO_05", "X1", "Processado e em carregamento para expedicao externa."),
        BuildPackage("PKG-0003", "Timon", "PM-B", "CAPITAL", PackageOperationalStatus.Buffer, PackageExceptionReason.AguardandoIrmao, "GEO_01", "A5", "Aguardando pacote irmao para consolidacao."),
        BuildPackage("PKG-0004", "Lagoa Alegre", "PM-C", "CAPITAL", PackageOperationalStatus.A_Caminho, PackageExceptionReason.SemRecebimentoNoSistema, null, null, "Chegou fisicamente ao SVC e ainda nao foi recebido no sistema."),
        BuildPackage("PKG-0005", "Monte Castelo", "PM-D", "CAPITAL", PackageOperationalStatus.Caso_PS, PackageExceptionReason.Avariado, "GEO_03", "A6", "Pacote segregado para tratativa de problema.")
    ];

    public IReadOnlyCollection<GeographicIsland> GetIslands() => _islands;

    public IReadOnlyCollection<PackageRecord> GetPackages() => _packages;

    public IReadOnlyCollection<PackageStatusSummary> GetPackageStatusSummary()
    {
        return _packages
            .GroupBy(package => package.Status)
            .Select(group => new PackageStatusSummary
            {
                StatusCode = group.Key.ToString(),
                StatusLabel = group.Key switch
                {
                    PackageOperationalStatus.A_Caminho => "A caminho",
                    PackageOperationalStatus.Para_Expedir => "Para expedir",
                    PackageOperationalStatus.Buffer => "Buffer",
                    PackageOperationalStatus.Em_Rota => "Em rota",
                    PackageOperationalStatus.Caso_PS => "Caso PS",
                    _ => group.Key.ToString()
                },
                Quantity = group.Count()
            })
            .OrderBy(summary => summary.StatusCode)
            .ToArray();
    }

    public GeographicIsland? FindByDestination(string destination)
    {
        var normalized = Normalize(destination);
        return _islands.FirstOrDefault(island => island.DestinationKey == normalized);
    }

    public PackageRecord? FindPackage(string barcode)
    {
        return _packages.FirstOrDefault(package =>
            package.Barcode.Equals(barcode.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public void RegisterPackage(string islandCode, string slotCode)
    {
        var island = _islands.First(island => island.Code.Equals(islandCode, StringComparison.OrdinalIgnoreCase));
        island.RegisterPackage(slotCode);
    }

    public void SavePackage(PackageRecord package)
    {
        var existing = FindPackage(package.Barcode);
        if (existing is null)
        {
            _packages.Add(package);
            return;
        }

        _packages.Remove(existing);
        _packages.Add(package);
    }

    private static string Normalize(string value)
    {
        return value.Trim().ToUpperInvariant();
    }

    private static PackageRecord BuildPackage(
        string barcode,
        string destination,
        string routeCode,
        string flowType,
        PackageOperationalStatus status,
        PackageExceptionReason exceptionReason,
        string? islandCode,
        string? slotCode,
        string description)
    {
        var package = new PackageRecord(
            barcode,
            destination,
            routeCode,
            flowType,
            status,
            exceptionReason,
            DateTime.UtcNow.AddMinutes(-15));

        package.UpdateOperationalState(
            status,
            exceptionReason,
            DateTime.UtcNow.AddMinutes(-15),
            description,
            islandCode,
            slotCode);

        return package;
    }
}
