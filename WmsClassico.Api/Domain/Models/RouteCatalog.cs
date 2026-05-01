namespace WmsClassico.Api.Domain.Models;

public static class RouteCatalog
{
    public static readonly IReadOnlyCollection<RouteDefinition> All =
    [
        new RouteDefinition { Code = "PM-A", FlowType = "CAPITAL", Region = "Capital", Description = "Rota capital A" },
        new RouteDefinition { Code = "PM-B", FlowType = "CAPITAL", Region = "Capital", Description = "Rota capital B" },
        new RouteDefinition { Code = "PM-C", FlowType = "CAPITAL", Region = "Capital", Description = "Rota capital C" },
        new RouteDefinition { Code = "PM-D", FlowType = "CAPITAL", Region = "Capital", Description = "Rota capital D" },
        new RouteDefinition { Code = "PM-F", FlowType = "CAPITAL", Region = "Capital", Description = "Rota capital F" },
        new RouteDefinition { Code = "PM-G", FlowType = "CAPITAL", Region = "Capital", Description = "Rota capital G" },
        new RouteDefinition { Code = "AM5-J", FlowType = "XPT", Region = "Balsas", Description = "XPT Balsas J" },
        new RouteDefinition { Code = "AM5-K", FlowType = "XPT", Region = "Balsas", Description = "XPT Balsas K" },
        new RouteDefinition { Code = "AM5-L", FlowType = "XPT", Region = "Urucui", Description = "XPT Urucui L" },
        new RouteDefinition { Code = "AM5-M", FlowType = "XPT", Region = "Urucui", Description = "XPT Urucui M" }
    ];

    public static RouteDefinition? Find(string routeCode)
    {
        return All.FirstOrDefault(route =>
            route.Code.Equals(routeCode.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}
