using WmsClassico.Api.Domain.Entities;
using WmsClassico.Api.Domain.Models;
using WmsClassico.Api.Infrastructure;

namespace WmsClassico.Api.Services;

public sealed class TriageEngine(
    IWarehouseRepository repository,
    IPokaYokeService pokaYokeService) : ITriageEngine
{
    private const decimal RedThreshold = 90m;
    private const decimal DistantIslandMaxXptShare = 55m;

    public TriageDecision ProcessCheckIn(PackageCheckInRequest request)
    {
        var flowType = NormalizeFlow(request.FlowType);
        var route = RouteCatalog.Find(request.RouteCode);
        var island = repository.FindByDestination(request.Destination);
        var alerts = new List<string>();
        var existingPackage = repository.FindPackage(request.Barcode);
        var pokaYokeAlerts = pokaYokeService.Evaluate(request, existingPackage);

        if (pokaYokeAlerts.Any(alert => alert.BlocksOperation))
        {
            return new TriageDecision
            {
                Barcode = request.Barcode,
                Destination = request.Destination,
                FlowType = flowType,
                RouteCode = request.RouteCode,
                IslandCode = "TRIAGEM_MANUAL",
                IslandName = "Triagem Manual",
                SlotCode = "POKA_YOKE",
                FlowStatus = "VERMELHO",
                AllowDirection = false,
                PriorityScore = CalculatePriority(request),
                Action = "Bloquear o pacote e conferir leitura, destino e status anterior.",
                Reason = "Poka Yoke detectou uma divergencia critica de bipagem.",
                PackageStatus = PackageOperationalStatus.Caso_PS,
                ExceptionReason = PackageExceptionReason.DivergenciaDeBipagem,
                Alerts = pokaYokeAlerts.Select(alert => alert.Message).ToArray(),
                PokaYokeAlerts = pokaYokeAlerts
            };
        }

        if (route is null)
        {
            alerts.Add("Rota fora do catalogo operacional.");
            return new TriageDecision
            {
                Barcode = request.Barcode,
                Destination = request.Destination,
                FlowType = flowType,
                RouteCode = request.RouteCode,
                IslandCode = "TRIAGEM_MANUAL",
                IslandName = "Triagem Manual",
                SlotCode = "REVISAR_ROTA",
                FlowStatus = "VERMELHO",
                AllowDirection = false,
                PriorityScore = CalculatePriority(request),
                Action = "Validar a rota antes de depositar o pacote.",
                Reason = "A rota informada nao pertence ao catalogo padrao PM/AM5.",
                PackageStatus = PackageOperationalStatus.Caso_PS,
                ExceptionReason = PackageExceptionReason.RotaInvalida,
                Alerts = alerts,
                PokaYokeAlerts = pokaYokeAlerts
            };
        }

        if (!route.FlowType.Equals(flowType, StringComparison.OrdinalIgnoreCase))
        {
            alerts.Add($"Fluxo {flowType} nao combina com a rota {route.Code}.");
            return new TriageDecision
            {
                Barcode = request.Barcode,
                Destination = request.Destination,
                FlowType = flowType,
                RouteCode = route.Code,
                IslandCode = "TRIAGEM_MANUAL",
                IslandName = "Triagem Manual",
                SlotCode = "ERRO_DE_MIX",
                FlowStatus = "VERMELHO",
                AllowDirection = false,
                PriorityScore = CalculatePriority(request),
                Action = "Segurar pacote e confirmar se houve erro de pesca ou cadastro.",
                Reason = $"A rota {route.Code} e do fluxo {route.FlowType}, mas o pacote foi informado como {flowType}.",
                PackageStatus = PackageOperationalStatus.Caso_PS,
                ExceptionReason = PackageExceptionReason.FluxoIncoerente,
                Alerts = alerts,
                PokaYokeAlerts = pokaYokeAlerts
            };
        }

        if (island is null)
        {
            return new TriageDecision
            {
                Barcode = request.Barcode,
                Destination = request.Destination,
                FlowType = flowType,
                RouteCode = request.RouteCode,
                IslandCode = "TRIAGEM_MANUAL",
                IslandName = "Triagem Manual",
                SlotCode = "PENDENTE",
                FlowStatus = "VERMELHO",
                AllowDirection = false,
                PriorityScore = CalculatePriority(request),
                Action = "Revisar cadastro e direcionar manualmente",
                Reason = "Destino sem ilha geografica mapeada.",
                PackageStatus = PackageOperationalStatus.Caso_PS,
                ExceptionReason = PackageExceptionReason.DestinoNaoMapeado,
                Alerts = [.. alerts, "Destino sem parametrizacao."],
                PokaYokeAlerts = pokaYokeAlerts
            };
        }

        if (island.OccupancyPercent > RedThreshold)
        {
            alerts.Add("Ilha acima de 90% de ocupacao.");
            return BlockDecision(request, island, flowType, alerts, "Bloquear direcionamento e acionar supervisor.", PackageExceptionReason.CapacidadeBloqueada, pokaYokeAlerts);
        }

        var slot = SelectSlot(island, request.DepartureDay, flowType, alerts);
        if (slot is null)
        {
            alerts.Add("Nao ha subarea livre para o mix atual.");
            return BlockDecision(request, island, flowType, alerts, "Enviar para area de contingencia operacional.", PackageExceptionReason.CapacidadeBloqueada, pokaYokeAlerts);
        }

        repository.RegisterPackage(island.Code, slot.Code);
        var package = existingPackage ?? new PackageRecord(
            request.Barcode,
            request.Destination.Trim(),
            route.Code,
            flowType,
            PackageOperationalStatus.Para_Expedir,
            PackageExceptionReason.Nenhum,
            request.CheckInAt);

        package.Destination = request.Destination.Trim();
        package.RouteCode = route.Code;
        package.FlowType = flowType;
        package.UpdateOperationalState(
            PackageOperationalStatus.Para_Expedir,
            PackageExceptionReason.Nenhum,
            request.CheckInAt,
            "Pacote recebido e roteado para ilha/subarea operacional.",
            island.Code,
            slot.Code);
        repository.SavePackage(package);

        if (slot.OccupancyPercent >= 80m)
        {
            alerts.Add($"Subarea {slot.Code} entrou em atencao.");
        }

        if (island.DistanceRank >= 4 && island.XptSharePercent >= DistantIslandMaxXptShare)
        {
            alerts.Add("Share de XPT na ilha distante chegou no limite operacional.");
        }

        return new TriageDecision
        {
            Barcode = request.Barcode,
            Destination = request.Destination,
            FlowType = flowType,
            RouteCode = route.Code,
            IslandCode = island.Code,
            IslandName = island.Name,
            SlotCode = slot.Code,
            FlowStatus = island.FlowStatusCode,
            AllowDirection = true,
            PriorityScore = CalculatePriority(request),
            Action = $"Depositar na ilha {island.Code}, subarea {slot.Code}.",
            Reason = BuildReason(island, slot, route, request.DepartureDay),
            PackageStatus = PackageOperationalStatus.Para_Expedir,
            ExceptionReason = PackageExceptionReason.Nenhum,
            Alerts = alerts,
            PokaYokeAlerts = pokaYokeAlerts
        };
    }

    private static TriageDecision BlockDecision(
        PackageCheckInRequest request,
        GeographicIsland island,
        string flowType,
        IReadOnlyCollection<string> alerts,
        string action,
        PackageExceptionReason exceptionReason,
        IReadOnlyCollection<PokaYokeAlert> pokaYokeAlerts)
    {
        return new TriageDecision
        {
            Barcode = request.Barcode,
            Destination = request.Destination,
            FlowType = flowType,
            RouteCode = request.RouteCode,
            IslandCode = island.Code,
            IslandName = island.Name,
            SlotCode = "BLOQUEADO",
            FlowStatus = "VERMELHO",
            AllowDirection = false,
            PriorityScore = CalculatePriority(request),
            Action = action,
            Reason = "Ilha sem capacidade segura para receber novo pacote.",
            PackageStatus = PackageOperationalStatus.Buffer,
            ExceptionReason = exceptionReason,
            Alerts = alerts,
            PokaYokeAlerts = pokaYokeAlerts
        };
    }

    private IslandSlot? SelectSlot(
        GeographicIsland island,
        DayOfWeek departureDay,
        string flowType,
        ICollection<string> alerts)
    {
        var candidates = island.Slots
            .Where(slot => slot.SupportsFlow(flowType))
            .Where(slot => slot.SupportsDay(departureDay))
            .Where(slot => slot.OccupancyPercent < RedThreshold)
            .ToList();

        if (island.DistanceRank >= 4 && flowType == "XPT" && island.XptSharePercent >= DistantIslandMaxXptShare)
        {
            alerts.Add("XPT ja ocupa mais do que o limite previsto na ilha distante.");
            candidates = candidates.Where(slot => slot.IsContingency).ToList();
        }

        if (candidates.Count == 0)
        {
            candidates = island.Slots
                .Where(slot => slot.SupportsFlow(flowType))
                .Where(slot => slot.OccupancyPercent < RedThreshold)
                .OrderBy(slot => slot.IsContingency)
                .ToList();
        }

        return candidates
            .OrderBy(slot => ScoreSlot(island, slot, departureDay, flowType))
            .FirstOrDefault();
    }

    private static decimal ScoreSlot(
        GeographicIsland island,
        IslandSlot slot,
        DayOfWeek departureDay,
        string flowType)
    {
        decimal score = slot.OccupancyPercent;

        if (!slot.SupportsDay(departureDay))
        {
            score += 20;
        }

        if (!slot.SupportsFlow(flowType))
        {
            score += 50;
        }

        if (slot.IsContingency)
        {
            score += 25;
        }

        if (island.DistanceRank >= 4)
        {
            score += island.XptSharePercent / 10;
        }

        return score;
    }

    private static int CalculatePriority(PackageCheckInRequest request)
    {
        var score = request.DepartureDay switch
        {
            DayOfWeek.Monday => 10,
            DayOfWeek.Tuesday => 9,
            DayOfWeek.Wednesday => 8,
            DayOfWeek.Thursday => 7,
            DayOfWeek.Friday => 6,
            _ => 5
        };

        if (NormalizeFlow(request.FlowType) == "XPT")
        {
            score += 1;
        }

        if (request.ServiceType.Equals("EXPRESSO", StringComparison.OrdinalIgnoreCase))
        {
            score += 3;
        }

        return score;
    }

    private static string BuildReason(
        GeographicIsland island,
        IslandSlot slot,
        RouteDefinition route,
        DayOfWeek departureDay)
    {
        if (island.DistanceRank >= 4)
        {
            return $"Ilha distante balanceada pela rota {route.Code}, fluxo {route.FlowType}, dia de saida {departureDay} e capacidade da subarea {slot.Code}.";
        }

        return $"Destino roteado para {island.Code} conforme mapa geografico, rota {route.Code} da regiao {route.Region} e janela de expedicao {departureDay}.";
    }

    private static string NormalizeFlow(string flowType)
    {
        return flowType.Trim().ToUpperInvariant() switch
        {
            "XPT" => "XPT",
            _ => "CAPITAL"
        };
    }
}
