using WmsClassico.Api.Domain.Entities;
using WmsClassico.Api.Domain.Models;

namespace WmsClassico.Api.Services;

public interface IPokaYokeService
{
    IReadOnlyCollection<PokaYokeAlert> Evaluate(PackageCheckInRequest request, PackageRecord? existingPackage);
}
