using WmsClassico.Api.Domain.Models;
using WmsClassico.Api.Infrastructure;
using WmsClassico.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<IWarehouseRepository, SqliteWarehouseRepository>();
builder.Services.AddSingleton<IPokaYokeService, PokaYokeService>();
builder.Services.AddSingleton<ITriageEngine, TriageEngine>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.MapGet("/api/islands", (IWarehouseRepository repository) =>
{
    var islands = repository.GetIslands()
        .Select(island => new
        {
            island.Code,
            island.Name,
            island.DestinationKey,
            island.DistanceRank,
            island.MaxCapacity,
            island.CurrentOccupancy,
            OccupancyPercent = island.OccupancyPercent,
            island.FlowStatusCode,
            island.Slots
        });

    return Results.Ok(islands);
});

app.MapGet("/api/packages", (IWarehouseRepository repository) =>
{
    var packages = repository.GetPackages()
        .OrderByDescending(package => package.LastUpdatedAt)
        .Select(package => new
        {
            package.Barcode,
            package.Destination,
            package.RouteCode,
            package.FlowType,
            Status = package.Status.ToString(),
            ExceptionReason = package.ExceptionReason.ToString(),
            package.LastIslandCode,
            package.LastSlotCode,
            package.BrotherBarcode,
            package.PlannedDepartureDate,
            package.LastUpdatedAt
        });

    return Results.Ok(packages);
});

app.MapGet("/api/packages/summary", (IWarehouseRepository repository) =>
{
    return Results.Ok(repository.GetPackageStatusSummary());
});

app.MapPost("/api/check-in/triage", (PackageCheckInRequest request, ITriageEngine triageEngine) =>
{
    var decision = triageEngine.ProcessCheckIn(request);
    return Results.Ok(decision);
});

app.Run();
