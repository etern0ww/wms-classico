using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using WmsClassico.Api.Domain.Models;
using WmsClassico.Api.Infrastructure;
using WmsClassico.Api.Presentation.Models;
using WmsClassico.Api.Services;

namespace WmsClassico.Api.Controllers;

public sealed class DashboardController(
    IWarehouseRepository repository,
    ITriageEngine triageEngine) : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        var model = BuildViewModel(new TriageFormModel());
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Triage(TriageFormModel form)
    {
        if (!ModelState.IsValid)
        {
            var invalidModel = BuildViewModel(form);
            return View("Index", invalidModel);
        }

        var request = new PackageCheckInRequest
        {
            Barcode = form.Barcode.Trim(),
            Destination = form.Destination.Trim(),
            FlowType = form.FlowType,
            RouteCode = form.RouteCode.Trim(),
            DepartureDay = form.DepartureDay,
            ServiceType = form.ServiceType.Trim(),
            CheckInAt = DateTime.UtcNow
        };

        var decision = triageEngine.ProcessCheckIn(request);
        var model = BuildViewModel(form, decision);

        return View("Index", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdatePackageStatus(string barcode, string newStatus, int quantity)
    {
        // Aqui você implementaria a lógica para atualizar o status dos pacotes
        // Por enquanto, apenas redireciona para a página inicial
        var form = new TriageFormModel();
        var model = BuildViewModel(form);
        
        TempData["Message"] = $"Atualizado {quantity} pacote(s) para status {newStatus}";
        return View("Index", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ToggleIslandClosed(string islandCode)
    {
        var form = new TriageFormModel();
        var model = BuildViewModel(form);
        
        TempData["Message"] = $"Ilha {islandCode} atualizada";
        return View("Index", model);
    }

    private DashboardViewModel BuildViewModel(TriageFormModel form, TriageDecision? decision = null)
    {
        var islands = repository.GetIslands()
            .OrderBy(island => island.DistanceRank)
            .Select(island => new IslandCardViewModel
            {
                Code = island.Code,
                Name = island.Name,
                DestinationKey = island.DestinationKey,
                DistanceRank = island.DistanceRank,
                CurrentOccupancy = island.CurrentOccupancy,
                MaxCapacity = island.MaxCapacity,
                OccupancyPercent = island.OccupancyPercent,
                FlowStatusCode = island.FlowStatusCode,
                XptSharePercent = island.XptSharePercent,
                Slots = island.Slots
                    .OrderBy(slot => slot.Code)
                    .Select(slot => new IslandSlotViewModel
                    {
                        Code = slot.Code,
                        SupportedFlow = slot.SupportedFlow,
                        CurrentOccupancy = slot.CurrentOccupancy,
                        MaxCapacity = slot.MaxCapacity,
                        OccupancyPercent = slot.OccupancyPercent,
                        AllowedDepartureDays = slot.AllowedDepartureDays.Select(FormatDayOfWeek).ToArray(),
                        IsContingency = slot.IsContingency
                    })
                    .ToArray()
            })
            .ToArray();

        return new DashboardViewModel
        {
            Form = form,
            Decision = decision,
            Islands = islands,
            Routes = RouteCatalog.All
                .Select(route => new SelectListItem
                {
                    Value = route.Code,
                    Text = $"{route.Code} - {route.Description}"
                })
                .ToArray(),
            PackageSummary = repository.GetPackageStatusSummary()
                .Select(summary => new PackageStatusSummaryViewModel
                {
                    StatusCode = summary.StatusCode,
                    StatusLabel = summary.StatusLabel,
                    Quantity = summary.Quantity
                })
                .ToArray(),
            Packages = repository.GetPackages()
                .OrderByDescending(package => package.LastUpdatedAt)
                .Select(package => new PackageTrackingViewModel
                {
                    Barcode = package.Barcode,
                    Destination = package.Destination,
                    RouteCode = package.RouteCode,
                    FlowType = package.FlowType,
                    StatusLabel = FormatPackageStatus(package.Status),
                    ExceptionReasonLabel = FormatExceptionReason(package.ExceptionReason),
                    LastIslandCode = package.LastIslandCode,
                    LastSlotCode = package.LastSlotCode,
                    BrotherBarcode = package.BrotherBarcode,
                    PlannedDepartureDate = package.PlannedDepartureDate?.ToString("dd/MM/yyyy"),
                    LastUpdatedAt = package.LastUpdatedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
                    IsProblemCase = package.Status == PackageOperationalStatus.Caso_PS
                })
                .ToArray(),
            PackageStatuses = repository.GetPackages()
                .Select(package => FormatPackageStatus(package.Status))
                .Distinct()
                .OrderBy(value => value)
                .ToArray(),
            PackageFlows = repository.GetPackages()
                .Select(package => package.FlowType)
                .Distinct()
                .OrderBy(value => value)
                .ToArray(),
            PackageRoutes = repository.GetPackages()
                .Select(package => package.RouteCode)
                .Distinct()
                .OrderBy(value => value)
                .ToArray(),
            
            // Indicadores circulares - calcular ondas baseadas nas ilhas
            TotalCages = islands.Sum(i => i.MaxCapacity),
            FullCages = islands.Sum(i => i.CurrentOccupancy),
            FullCagesPercent = islands.Sum(i => i.MaxCapacity) > 0 
                ? Math.Round((decimal)islands.Sum(i => i.CurrentOccupancy) / islands.Sum(i => i.MaxCapacity) * 100, 1)
                : 0,
            WaveOccupancy = CalculateWaveOccupancy(islands),
            
            // Logs Poka Yoke - exemplo padrão com João
            PokaYokeLogs = GetPokaYokeLogs(),
            
            // Novas funcionalidades
            Cages = GetCages(islands),
            IslandsClosed = GetIslandsClosed(islands)
        };
    }
    
    private static IReadOnlyList<CageViewModel> GetCages(IReadOnlyList<IslandCardViewModel> islands)
    {
        // Criar gaiolas baseadas nas ilhas (cada ilha pode ter múltiplas gaiolas)
        var cages = new List<CageViewModel>();
        var random = new Random();
        
        foreach (var island in islands)
        {
            // Criar 2-3 gaiolas por ilha
            var cageCount = random.Next(2, 4);
            for (int i = 1; i <= cageCount; i++)
            {
                var capacity = island.MaxCapacity / cageCount;
                var occupancy = random.Next(0, capacity);
                var isReady = occupancy >= capacity * 0.8;
                
                cages.Add(new CageViewModel
                {
                    Code = $"{island.Code}-G{i}",
                    Name = $"Gaiola {i} - {island.Name}",
                    CurrentOccupancy = occupancy,
                    MaxCapacity = capacity,
                    OccupancyPercent = capacity > 0 ? Math.Round((decimal)occupancy / capacity * 100, 1) : 0,
                    WaveNumber = island.DistanceRank,
                    IsReadyForDispatch = isReady
                });
            }
        }
        
        return cages.OrderBy(c => c.WaveNumber).ThenBy(c => c.Code).ToArray();
    }
    
    private static IReadOnlyList<IslandClosedViewModel> GetIslandsClosed(IReadOnlyList<IslandCardViewModel> islands)
    {
        var random = new Random();
        return islands
            .Where((island, index) => index % 2 == 0) // Metade das ilhas como "fechadas"
            .Select(island => new IslandClosedViewModel
            {
                IslandCode = island.Code,
                IslandName = island.Name,
                IsClosed = random.Next(2) == 1,
                IsReadyForDispatch = island.OccupancyPercent >= 80,
                ClosedAt = random.Next(2) == 1 ? DateTime.Now.AddHours(-random.Next(1, 6)) : null,
                FormattedClosedAt = ""
            })
            .Select(island => new IslandClosedViewModel
            {
                IslandCode = island.IslandCode,
                IslandName = island.IslandName,
                IsClosed = island.IsClosed,
                IsReadyForDispatch = island.IsReadyForDispatch,
                ClosedAt = island.ClosedAt,
                FormattedClosedAt = island.ClosedAt?.ToString("dd/MM HH:mm") ?? ""
            })
            .ToArray();
    }
    
    private static IReadOnlyList<WaveOccupancyViewModel> CalculateWaveOccupancy(IReadOnlyList<IslandCardViewModel> islands)
    {
        // Agrupar ilhas por onda (A+B = 1, C+D = 2, etc)
        var waveGroups = new Dictionary<int, List<IslandCardViewModel>>();
        
        foreach (var island in islands)
        {
            // Extrair número da onda do código da ilha (GEO_01 = onda 1, GEO_02 = onda 1, etc)
            var waveNumber = island.DistanceRank;
            
            if (!waveGroups.ContainsKey(waveNumber))
            {
                waveGroups[waveNumber] = new List<IslandCardViewModel>();
            }
            waveGroups[waveNumber].Add(island);
        }
        
        return waveGroups
            .OrderBy(g => g.Key)
            .Select(g => new WaveOccupancyViewModel
            {
                WaveNumber = g.Key,
                WaveName = $"Onda {g.Key}",
                Islands = g.Value.Select(i => i.Code).ToArray(),
                TotalCapacity = g.Value.Sum(i => i.MaxCapacity),
                CurrentOccupancy = g.Value.Sum(i => i.CurrentOccupancy),
                OccupancyPercent = g.Value.Sum(i => i.MaxCapacity) > 0
                    ? Math.Round((decimal)g.Value.Sum(i => i.CurrentOccupancy) / g.Value.Sum(i => i.MaxCapacity) * 100, 1)
                    : 0
            })
            .ToArray();
    }
    
    private static IReadOnlyList<PokaYokeLogViewModel> GetPokaYokeLogs()
    {
        // Log de exemplo com 10 usuários
        var now = DateTime.Now;
        var baseTime = now.AddMinutes(-5);
        
        return new List<PokaYokeLogViewModel>
        {
            new PokaYokeLogViewModel
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "João",
                Action = "Login realizado",
                Timestamp = now,
                FormattedTime = now.ToString("HH:mm"),
                FormattedDate = now.ToString("dd/MM/yyyy")
            },
            new PokaYokeLogViewModel
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "Gabriel",
                Action = "Pacote bipado",
                Timestamp = baseTime.AddMinutes(-2),
                FormattedTime = baseTime.AddMinutes(-2).ToString("HH:mm"),
                FormattedDate = now.ToString("dd/MM/yyyy")
            },
            new PokaYokeLogViewModel
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "Elaine",
                Action = "Rota conferida",
                Timestamp = baseTime.AddMinutes(-4),
                FormattedTime = baseTime.AddMinutes(-4).ToString("HH:mm"),
                FormattedDate = now.ToString("dd/MM/yyyy")
            },
            new PokaYokeLogViewModel
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "Davi",
                Action = "Ilha fechada",
                Timestamp = baseTime.AddMinutes(-6),
                FormattedTime = baseTime.AddMinutes(-6).ToString("HH:mm"),
                FormattedDate = now.ToString("dd/MM/yyyy")
            },
            new PokaYokeLogViewModel
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "Carol",
                Action = "Gaiola organizada",
                Timestamp = baseTime.AddMinutes(-8),
                FormattedTime = baseTime.AddMinutes(-8).ToString("HH:mm"),
                FormattedDate = now.ToString("dd/MM/yyyy")
            },
            new PokaYokeLogViewModel
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "Stanley",
                Action = "Pacote exceptionado",
                Timestamp = baseTime.AddMinutes(-10),
                FormattedTime = baseTime.AddMinutes(-10).ToString("HH:mm"),
                FormattedDate = now.ToString("dd/MM/yyyy")
            },
            new PokaYokeLogViewModel
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "Thaylison",
                Action = "Status atualizado",
                Timestamp = baseTime.AddMinutes(-12),
                FormattedTime = baseTime.AddMinutes(-12).ToString("HH:mm"),
                FormattedDate = now.ToString("dd/MM/yyyy")
            },
            new PokaYokeLogViewModel
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "Joyci",
                Action = "Triagem concluída",
                Timestamp = baseTime.AddMinutes(-14),
                FormattedTime = baseTime.AddMinutes(-14).ToString("HH:mm"),
                FormattedDate = now.ToString("dd/MM/yyyy")
            },
            new PokaYokeLogViewModel
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "Jose",
                Action = "Logout realizado",
                Timestamp = baseTime.AddMinutes(-16),
                FormattedTime = baseTime.AddMinutes(-16).ToString("HH:mm"),
                FormattedDate = now.ToString("dd/MM/yyyy")
            },
            new PokaYokeLogViewModel
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "Uriel",
                Action = "Pacote conferido",
                Timestamp = baseTime.AddMinutes(-18),
                FormattedTime = baseTime.AddMinutes(-18).ToString("HH:mm"),
                FormattedDate = now.ToString("dd/MM/yyyy")
            }
        };
    }

    private static string FormatDayOfWeek(DayOfWeek dayOfWeek)
    {
        return dayOfWeek switch
        {
            DayOfWeek.Monday => "Segunda",
            DayOfWeek.Tuesday => "Terca",
            DayOfWeek.Wednesday => "Quarta",
            DayOfWeek.Thursday => "Quinta",
            DayOfWeek.Friday => "Sexta",
            DayOfWeek.Saturday => "Sabado",
            DayOfWeek.Sunday => "Domingo",
            _ => dayOfWeek.ToString()
        };
    }

    private static string FormatPackageStatus(PackageOperationalStatus status)
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

    private static string FormatExceptionReason(PackageExceptionReason reason)
    {
        return reason switch
        {
            PackageExceptionReason.Nenhum => "Sem excecao",
            PackageExceptionReason.EnvioCancelado => "Envio cancelado",
            PackageExceptionReason.Avariado => "Avariado",
            PackageExceptionReason.RotaInvalida => "Rota invalida",
            PackageExceptionReason.FluxoIncoerente => "Fluxo incoerente",
            PackageExceptionReason.DestinoNaoMapeado => "Destino nao mapeado",
            PackageExceptionReason.CapacidadeBloqueada => "Capacidade bloqueada",
            PackageExceptionReason.AguardandoIrmao => "Aguardando irmao",
            PackageExceptionReason.AguardandoDataDeSaida => "Aguardando data",
            PackageExceptionReason.SemRecebimentoNoSistema => "Sem recebimento no sistema",
            PackageExceptionReason.DivergenciaDeBipagem => "Divergencia de bipagem",
            _ => reason.ToString()
        };
    }
}
