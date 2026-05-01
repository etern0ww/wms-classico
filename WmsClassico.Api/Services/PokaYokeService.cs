using WmsClassico.Api.Domain.Entities;
using WmsClassico.Api.Domain.Models;

namespace WmsClassico.Api.Services;

public sealed class PokaYokeService : IPokaYokeService
{
    public IReadOnlyCollection<PokaYokeAlert> Evaluate(PackageCheckInRequest request, PackageRecord? existingPackage)
    {
        var alerts = new List<PokaYokeAlert>();

        if (existingPackage is not null &&
            existingPackage.Status is PackageOperationalStatus.Em_Rota or PackageOperationalStatus.Caso_PS)
        {
            alerts.Add(new PokaYokeAlert
            {
                Code = "DUPLICATE_FINAL_STATE",
                Message = $"Pacote {request.Barcode} ja esta em estado {existingPackage.Status}. Conferir bipagem duplicada.",
                BlocksOperation = true
            });
        }

        if (existingPackage is not null &&
            !existingPackage.Destination.Equals(request.Destination.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            alerts.Add(new PokaYokeAlert
            {
                Code = "DESTINATION_DIVERGENCE",
                Message = "Destino informado difere do ultimo destino registrado para este pacote.",
                BlocksOperation = true
            });
        }

        if (request.Barcode.Length < 6)
        {
            alerts.Add(new PokaYokeAlert
            {
                Code = "BARCODE_SUSPECT",
                Message = "Codigo de barras muito curto para o padrao operacional.",
                BlocksOperation = true
            });
        }

        return alerts;
    }
}
