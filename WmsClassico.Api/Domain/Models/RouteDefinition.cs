namespace WmsClassico.Api.Domain.Models;

public sealed class RouteDefinition
{
    public required string Code { get; init; }
    public required string FlowType { get; init; }
    public required string Region { get; init; }
    public required string Description { get; init; }
}
