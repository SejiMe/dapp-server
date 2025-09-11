using dengue.watch.api.common.interfaces;
using Microsoft.AspNetCore.Mvc;

namespace dengue.watch.api.features.denguealerts;

/// <summary>
/// Dengue alert endpoints
/// </summary>
public class DengueAlertEndpoints : IEndpoint
{
    public static IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/dengue-alerts")
            .WithTags("Dengue Alerts")
            .WithOpenApi();

        // GET /api/dengue-alerts
        group.MapGet("/", GetActiveAlerts)
            .WithName("GetActiveAlerts")
            .WithSummary("Get all active dengue alerts")
            .Produces<IEnumerable<DengueAlertDto>>();

        // GET /api/dengue-alerts/{id}
        group.MapGet("/{id:int}", GetAlertById)
            .WithName("GetAlertById")
            .WithSummary("Get a dengue alert by ID")
            .Produces<DengueAlertDto>()
            .Produces(404);

        // GET /api/dengue-alerts/location/{location}
        group.MapGet("/location/{location}", GetAlertsByLocation)
            .WithName("GetAlertsByLocation")
            .WithSummary("Get dengue alerts by location")
            .Produces<IEnumerable<DengueAlertDto>>();

        // POST /api/dengue-alerts
        group.MapPost("/", CreateAlert)
            .WithName("CreateAlert")
            .WithSummary("Create a new dengue alert")
            .Produces<DengueAlertDto>(201)
            .Produces(400);

        // PUT /api/dengue-alerts/{id}
        group.MapPut("/{id:int}", UpdateAlert)
            .WithName("UpdateAlert")
            .WithSummary("Update an existing dengue alert")
            .Produces<DengueAlertDto>()
            .Produces(404)
            .Produces(400);

        // POST /api/dengue-alerts/{id}/resolve
        group.MapPost("/{id:int}/resolve", ResolveAlert)
            .WithName("ResolveAlert")
            .WithSummary("Resolve a dengue alert")
            .Produces(200)
            .Produces(404);

        // DELETE /api/dengue-alerts/{id}
        group.MapDelete("/{id:int}", DeleteAlert)
            .WithName("DeleteAlert")
            .WithSummary("Delete a dengue alert")
            .Produces(204)
            .Produces(404);

        return app;
    }

    private static async Task<IResult> GetActiveAlerts(
        IDengueAlertService service,
        CancellationToken cancellationToken)
    {
        var alerts = await service.GetActiveAlertsAsync(cancellationToken);
        return Results.Ok(alerts);
    }

    private static async Task<IResult> GetAlertById(
        int id,
        IDengueAlertService service,
        CancellationToken cancellationToken)
    {
        var alert = await service.GetAlertByIdAsync(id, cancellationToken);
        return Results.Ok(alert);
    }

    private static async Task<IResult> GetAlertsByLocation(
        string location,
        IDengueAlertService service,
        CancellationToken cancellationToken)
    {
        var alerts = await service.GetAlertsByLocationAsync(location, cancellationToken);
        return Results.Ok(alerts);
    }

    private static async Task<IResult> CreateAlert(
        [FromBody] CreateDengueAlertRequest request,
        IDengueAlertService service,
        CancellationToken cancellationToken)
    {
        var alert = await service.CreateAlertAsync(request, cancellationToken);
        return Results.Created($"/api/dengue-alerts/{alert.Id}", alert);
    }

    private static async Task<IResult> UpdateAlert(
        int id,
        [FromBody] UpdateDengueAlertRequest request,
        IDengueAlertService service,
        CancellationToken cancellationToken)
    {
        var alert = await service.UpdateAlertAsync(id, request, cancellationToken);
        return Results.Ok(alert);
    }

    private static async Task<IResult> ResolveAlert(
        int id,
        IDengueAlertService service,
        CancellationToken cancellationToken)
    {
        await service.ResolveAlertAsync(id, cancellationToken);
        return Results.Ok(new { message = "Alert resolved successfully" });
    }

    private static async Task<IResult> DeleteAlert(
        int id,
        IDengueAlertService service,
        CancellationToken cancellationToken)
    {
        await service.DeleteAlertAsync(id, cancellationToken);
        return Results.NoContent();
    }
}
