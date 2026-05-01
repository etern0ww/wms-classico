using Microsoft.Data.Sqlite;
using WmsClassico.Api.Domain.Entities;
using WmsClassico.Api.Domain.Models;

namespace WmsClassico.Api.Infrastructure;

public sealed class SqliteWarehouseRepository : IWarehouseRepository
{
    private readonly string _connectionString;
    private readonly object _syncRoot = new();

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
                new IslandSlot { Code = "A3", SupportedFlow = "CAPITAL", AllowedDepartureDays = [DayOfWeek.Monday, DayOfWeek.Tuesday], MaxCapacity = 110, CurrentOccupancy = 62 },
                new IslandSlot { Code = "A4", SupportedFlow = "CAPITAL", AllowedDepartureDays = [DayOfWeek.Wednesday, DayOfWeek.Thursday], MaxCapacity = 110, CurrentOccupancy = 58 },
                new IslandSlot { Code = "A9", SupportedFlow = "CAPITAL", AllowedDepartureDays = [DayOfWeek.Friday, DayOfWeek.Saturday], MaxCapacity = 120, CurrentOccupancy = 77 },
                new IslandSlot { Code = "X1", SupportedFlow = "XPT", AllowedDepartureDays = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday], MaxCapacity = 90, CurrentOccupancy = 78 },
                new IslandSlot { Code = "X2", SupportedFlow = "XPT", AllowedDepartureDays = [DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday], MaxCapacity = 70, CurrentOccupancy = 66 },
                new IslandSlot { Code = "PULMAO_XPT", SupportedFlow = "XPT", AllowedDepartureDays = [], MaxCapacity = 20, CurrentOccupancy = 5, IsContingency = true }
            ]
        }
    ];

    public SqliteWarehouseRepository()
    {
        var dataDirectory = Path.Combine(AppContext.BaseDirectory, "App_Data");
        Directory.CreateDirectory(dataDirectory);

        var databasePath = Path.Combine(dataDirectory, "wms-classico.db");
        _connectionString = $"Data Source={databasePath}";

        EnsureDatabase();
        SeedPackagesIfNeeded();
    }

    public IReadOnlyCollection<GeographicIsland> GetIslands() => _islands;

    public IReadOnlyCollection<PackageRecord> GetPackages()
    {
        lock (_syncRoot)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT barcode, destino, rota_codigo, flow_type, status_operacional, motivo_excecao,
                       brother_barcode, data_saida_planejada, ilha_atual, subarea_atual, atualizado_em
                FROM Pacotes
                ORDER BY atualizado_em DESC
                """;

            using var reader = command.ExecuteReader();
            var packages = new List<PackageRecord>();

            while (reader.Read())
            {
                var package = HydratePackage(reader);
                package.Events.AddRange(GetEvents(connection, package.Barcode));
                packages.Add(package);
            }

            return packages;
        }
    }

    public IReadOnlyCollection<PackageStatusSummary> GetPackageStatusSummary()
    {
        lock (_syncRoot)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT status_operacional, COUNT(1)
                FROM Pacotes
                GROUP BY status_operacional
                ORDER BY status_operacional
                """;

            using var reader = command.ExecuteReader();
            var summary = new List<PackageStatusSummary>();

            while (reader.Read())
            {
                var status = Enum.Parse<PackageOperationalStatus>(reader.GetString(0));
                summary.Add(new PackageStatusSummary
                {
                    StatusCode = status.ToString(),
                    StatusLabel = FormatStatus(status),
                    Quantity = reader.GetInt32(1)
                });
            }

            return summary;
        }
    }

    public GeographicIsland? FindByDestination(string destination)
    {
        var normalized = Normalize(destination);
        return _islands.FirstOrDefault(island => island.DestinationKey == normalized);
    }

    public PackageRecord? FindPackage(string barcode)
    {
        lock (_syncRoot)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT barcode, destino, rota_codigo, flow_type, status_operacional, motivo_excecao,
                       brother_barcode, data_saida_planejada, ilha_atual, subarea_atual, atualizado_em
                FROM Pacotes
                WHERE barcode = $barcode
                LIMIT 1
                """;
            command.Parameters.AddWithValue("$barcode", barcode.Trim());

            using var reader = command.ExecuteReader();
            if (!reader.Read())
            {
                return null;
            }

            var package = HydratePackage(reader);
            package.Events.AddRange(GetEvents(connection, package.Barcode));
            return package;
        }
    }

    public void RegisterPackage(string islandCode, string slotCode)
    {
        var island = _islands.First(island => island.Code.Equals(islandCode, StringComparison.OrdinalIgnoreCase));
        island.RegisterPackage(slotCode);
    }

    public void SavePackage(PackageRecord package)
    {
        lock (_syncRoot)
        {
            using var connection = OpenConnection();
            using var transaction = connection.BeginTransaction();

            using var upsert = connection.CreateCommand();
            upsert.Transaction = transaction;
            upsert.CommandText = """
                INSERT INTO Pacotes (
                    barcode, destino, rota_codigo, flow_type, status_operacional, motivo_excecao,
                    brother_barcode, data_saida_planejada, ilha_atual, subarea_atual, atualizado_em
                )
                VALUES (
                    $barcode, $destino, $rota, $flow, $status, $motivo,
                    $brother, $saida, $ilha, $subarea, $updated
                )
                ON CONFLICT(barcode) DO UPDATE SET
                    destino = excluded.destino,
                    rota_codigo = excluded.rota_codigo,
                    flow_type = excluded.flow_type,
                    status_operacional = excluded.status_operacional,
                    motivo_excecao = excluded.motivo_excecao,
                    brother_barcode = excluded.brother_barcode,
                    data_saida_planejada = excluded.data_saida_planejada,
                    ilha_atual = excluded.ilha_atual,
                    subarea_atual = excluded.subarea_atual,
                    atualizado_em = excluded.atualizado_em
                """;

            FillPackageParameters(upsert, package);
            upsert.ExecuteNonQuery();

            var latestEvent = package.Events.OrderByDescending(evt => evt.Timestamp).FirstOrDefault();
            if (latestEvent is not null)
            {
                using var insertEvent = connection.CreateCommand();
                insertEvent.Transaction = transaction;
                insertEvent.CommandText = """
                    INSERT INTO Pacote_Eventos (
                        barcode, status_operacional, motivo_excecao, descricao, ilha, subarea, criado_em
                    )
                    VALUES ($barcode, $status, $motivo, $descricao, $ilha, $subarea, $criado)
                    """;
                insertEvent.Parameters.AddWithValue("$barcode", package.Barcode);
                insertEvent.Parameters.AddWithValue("$status", latestEvent.Status.ToString());
                insertEvent.Parameters.AddWithValue("$motivo", latestEvent.ExceptionReason.ToString());
                insertEvent.Parameters.AddWithValue("$descricao", latestEvent.Description);
                insertEvent.Parameters.AddWithValue("$ilha", (object?)latestEvent.IslandCode ?? DBNull.Value);
                insertEvent.Parameters.AddWithValue("$subarea", (object?)latestEvent.SlotCode ?? DBNull.Value);
                insertEvent.Parameters.AddWithValue("$criado", latestEvent.Timestamp.ToString("O"));
                insertEvent.ExecuteNonQuery();
            }

            transaction.Commit();
        }
    }

    private void EnsureDatabase()
    {
        lock (_syncRoot)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                CREATE TABLE IF NOT EXISTS Pacotes (
                    barcode TEXT PRIMARY KEY,
                    destino TEXT NOT NULL,
                    rota_codigo TEXT NOT NULL,
                    flow_type TEXT NOT NULL,
                    status_operacional TEXT NOT NULL,
                    motivo_excecao TEXT NOT NULL,
                    brother_barcode TEXT NULL,
                    data_saida_planejada TEXT NULL,
                    ilha_atual TEXT NULL,
                    subarea_atual TEXT NULL,
                    atualizado_em TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS Pacote_Eventos (
                    evento_id INTEGER PRIMARY KEY AUTOINCREMENT,
                    barcode TEXT NOT NULL,
                    status_operacional TEXT NOT NULL,
                    motivo_excecao TEXT NOT NULL,
                    descricao TEXT NOT NULL,
                    ilha TEXT NULL,
                    subarea TEXT NULL,
                    criado_em TEXT NOT NULL
                );
                """;
            command.ExecuteNonQuery();
        }
    }

    private void SeedPackagesIfNeeded()
    {
        lock (_syncRoot)
        {
            using var connection = OpenConnection();
            using var countCommand = connection.CreateCommand();
            countCommand.CommandText = "SELECT COUNT(1) FROM Pacotes";
            var count = Convert.ToInt32(countCommand.ExecuteScalar());
            if (count > 0)
            {
                return;
            }

            foreach (var package in BuildSeedPackages())
            {
                SavePackage(package);
            }
        }
    }

    private SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    private static PackageRecord HydratePackage(SqliteDataReader reader)
    {
        var package = new PackageRecord(
            reader.GetString(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            Enum.Parse<PackageOperationalStatus>(reader.GetString(4)),
            Enum.Parse<PackageExceptionReason>(reader.GetString(5)),
            DateTime.Parse(reader.GetString(10)));

        var brotherBarcode = reader.IsDBNull(6) ? null : reader.GetString(6);
        DateOnly? departureDate = reader.IsDBNull(7)
            ? null
            : DateOnly.Parse(reader.GetString(7));
        var islandCode = reader.IsDBNull(8) ? null : reader.GetString(8);
        var slotCode = reader.IsDBNull(9) ? null : reader.GetString(9);

        package.UpdateOperationalState(
            package.Status,
            package.ExceptionReason,
            package.LastUpdatedAt,
            "Estado atual recuperado do banco.",
            islandCode,
            slotCode,
            departureDate,
            brotherBarcode);

        package.Events.Clear();
        return package;
    }

    private static IReadOnlyCollection<PackageTrackingEvent> GetEvents(SqliteConnection connection, string barcode)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT status_operacional, motivo_excecao, descricao, ilha, subarea, criado_em
            FROM Pacote_Eventos
            WHERE barcode = $barcode
            ORDER BY criado_em DESC
            """;
        command.Parameters.AddWithValue("$barcode", barcode);

        using var reader = command.ExecuteReader();
        var events = new List<PackageTrackingEvent>();
        while (reader.Read())
        {
            events.Add(new PackageTrackingEvent
            {
                Status = Enum.Parse<PackageOperationalStatus>(reader.GetString(0)),
                ExceptionReason = Enum.Parse<PackageExceptionReason>(reader.GetString(1)),
                Description = reader.GetString(2),
                IslandCode = reader.IsDBNull(3) ? null : reader.GetString(3),
                SlotCode = reader.IsDBNull(4) ? null : reader.GetString(4),
                Timestamp = DateTime.Parse(reader.GetString(5))
            });
        }

        return events;
    }

    private static void FillPackageParameters(SqliteCommand command, PackageRecord package)
    {
        command.Parameters.AddWithValue("$barcode", package.Barcode);
        command.Parameters.AddWithValue("$destino", package.Destination);
        command.Parameters.AddWithValue("$rota", package.RouteCode);
        command.Parameters.AddWithValue("$flow", package.FlowType);
        command.Parameters.AddWithValue("$status", package.Status.ToString());
        command.Parameters.AddWithValue("$motivo", package.ExceptionReason.ToString());
        command.Parameters.AddWithValue("$brother", (object?)package.BrotherBarcode ?? DBNull.Value);
        command.Parameters.AddWithValue(
            "$saida",
            package.PlannedDepartureDate is DateOnly departureDate
                ? departureDate.ToString("yyyy-MM-dd")
                : DBNull.Value);
        command.Parameters.AddWithValue("$ilha", (object?)package.LastIslandCode ?? DBNull.Value);
        command.Parameters.AddWithValue("$subarea", (object?)package.LastSlotCode ?? DBNull.Value);
        command.Parameters.AddWithValue("$updated", package.LastUpdatedAt.ToString("O"));
    }

    private static string Normalize(string value) => value.Trim().ToUpperInvariant();

    private static string FormatStatus(PackageOperationalStatus status)
    {
        return status switch
        {
            PackageOperationalStatus.A_Caminho => "A caminho",
            PackageOperationalStatus.Para_Expedir => "Para expedir",
            PackageOperationalStatus.Buffer => "Buffer",
            PackageOperationalStatus.Em_Rota => "Em rota",
            PackageOperationalStatus.Caso_PS => "Caso PS",
            _ => status.ToString()
        };
    }

    private static IReadOnlyCollection<PackageRecord> BuildSeedPackages()
    {
        return
        [
            BuildPackage("PKG-0001", "Jose de Freitas", "PM-A", "CAPITAL", PackageOperationalStatus.Para_Expedir, PackageExceptionReason.Nenhum, "GEO_05", "A3", "Recebido e aguardando composicao de rota."),
            BuildPackage("PKG-0002", "Jose de Freitas", "AM5-J", "XPT", PackageOperationalStatus.Em_Rota, PackageExceptionReason.Nenhum, "GEO_05", "X1", "Processado e em carregamento para expedicao externa."),
            BuildPackage("PKG-0003", "Timon", "PM-B", "CAPITAL", PackageOperationalStatus.Buffer, PackageExceptionReason.AguardandoIrmao, "GEO_01", "A5", "Aguardando pacote irmao para consolidacao.", brotherBarcode: "PKG-0099"),
            BuildPackage("PKG-0004", "Lagoa Alegre", "PM-C", "CAPITAL", PackageOperationalStatus.A_Caminho, PackageExceptionReason.SemRecebimentoNoSistema, null, null, "Chegou fisicamente ao SVC e ainda nao foi recebido no sistema."),
            BuildPackage("PKG-0005", "Monte Castelo", "PM-D", "CAPITAL", PackageOperationalStatus.Caso_PS, PackageExceptionReason.Avariado, "GEO_03", "A6", "Pacote segregado para tratativa de problema.")
        ];
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
        string description,
        DateOnly? departureDate = null,
        string? brotherBarcode = null)
    {
        var now = DateTime.UtcNow.AddMinutes(-15);
        var package = new PackageRecord(barcode, destination, routeCode, flowType, status, exceptionReason, now);
        package.UpdateOperationalState(status, exceptionReason, now, description, islandCode, slotCode, departureDate, brotherBarcode);
        return package;
    }
}
