namespace WmsClassico.Api.Domain.Models;

public sealed class PokaYokeAlert
{
    public required string Code { get; init; }
    public required string Message { get; init; }
    public bool BlocksOperation { get; init; }
}
