using WmsClassico.Api.Domain.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace WmsClassico.Api.Presentation.Models;

public sealed class DashboardViewModel
{
    public required TriageFormModel Form { get; init; }
    public TriageDecision? Decision { get; init; }
    public IReadOnlyCollection<IslandCardViewModel> Islands { get; init; } = [];
    public IReadOnlyCollection<SelectListItem> Routes { get; init; } = [];
    public IReadOnlyCollection<PackageStatusSummaryViewModel> PackageSummary { get; init; } = [];
    public IReadOnlyCollection<PackageTrackingViewModel> Packages { get; init; } = [];
    public IReadOnlyCollection<string> PackageStatuses { get; init; } = [];
    public IReadOnlyCollection<string> PackageFlows { get; init; } = [];
    public IReadOnlyCollection<string> PackageRoutes { get; init; } = [];
    
    // Indicadores circulares
    public int TotalCages { get; init; }
    public int FullCages { get; init; }
    public decimal FullCagesPercent { get; init; }
    public IReadOnlyCollection<WaveOccupancyViewModel> WaveOccupancy { get; init; } = [];
    
    // Logs Poka Yoke
    public IReadOnlyCollection<PokaYokeLogViewModel> PokaYokeLogs { get; init; } = [];
    
    // Novas funcionalidades
    public IReadOnlyCollection<CageViewModel> Cages { get; init; } = [];
    public IReadOnlyCollection<IslandClosedViewModel> IslandsClosed { get; init; } = [];
}
