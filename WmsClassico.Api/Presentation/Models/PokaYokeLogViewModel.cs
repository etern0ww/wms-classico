namespace WmsClassico.Api.Presentation.Models;

public sealed class PokaYokeLogViewModel
{
    public required string Id { get; init; }
    public required string UserName { get; init; }
    public required string Action { get; init; }
    public required DateTime Timestamp { get; init; }
    public required string FormattedTime { get; init; }
    public required string FormattedDate { get; init; }
}