using System.ComponentModel.DataAnnotations;

namespace WmsClassico.Api.Presentation.Models;

public sealed class TriageFormModel
{
    [Required]
    [Display(Name = "Codigo de barras")]
    public string Barcode { get; init; } = "PKG-0001";

    [Required]
    [Display(Name = "Destino")]
    public string Destination { get; init; } = "Jose de Freitas";

    [Required]
    [Display(Name = "Tipo de fluxo")]
    public string FlowType { get; init; } = "CAPITAL";

    [Required]
    [Display(Name = "Rota")]
    public string RouteCode { get; init; } = "PM-A";

    [Display(Name = "Dia de saida")]
    public DayOfWeek DepartureDay { get; init; } = DayOfWeek.Monday;

    [Required]
    [Display(Name = "Tipo de servico")]
    public string ServiceType { get; init; } = "PADRAO";
}
