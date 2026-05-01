using WmsClassico.Api.Domain.Models;

namespace WmsClassico.Api.Services;

public interface ITriageEngine
{
    TriageDecision ProcessCheckIn(PackageCheckInRequest request);
}
