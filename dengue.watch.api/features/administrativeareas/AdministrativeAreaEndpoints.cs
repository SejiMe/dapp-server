using dengue.watch.api.common.interfaces;

namespace dengue.watch.api.features.administrativeareas;

public class AdministrativeAreaEndpoints : IEndpoint
{
    public static IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/administrative-areas")
            .WithTags("Administrative Areas")
            .WithOpenApi();

        // Regions
        group.MapGet("/regions", GetRegions)
            .WithName("GetRegions")
            .WithSummary("Get all regions")
            .Produces<IEnumerable<AdministrativeAreaDto>>();

        // Provinces
        group.MapGet("/provinces", GetProvinces)
            .WithName("GetProvinces")
            .WithSummary("Get all provinces")
            .Produces<IEnumerable<AdministrativeAreaDto>>();

        group.MapGet("/regions/{regionPsgcCode}/provinces", GetProvincesByRegion)
            .WithName("GetProvincesByRegion")
            .WithSummary("Get all provinces under a region (GeographicLevel must be region)")
            .Produces<IEnumerable<AdministrativeAreaDto>>();

        // Cities
        group.MapGet("/cities", GetCities)
            .WithName("GetCities")
            .WithSummary("Get all cities")
            .Produces<IEnumerable<AdministrativeAreaDto>>();

        group.MapGet("/provinces/{provincePsgcCode}/cities", GetCitiesByProvince)
            .WithName("GetCitiesByProvince")
            .WithSummary("Get all cities under a province")
            .Produces<IEnumerable<AdministrativeAreaDto>>();

        // Municipalities
        group.MapGet("/municipalities", GetMunicipalities)
            .WithName("GetMunicipalities")
            .WithSummary("Get all municipalities")
            .Produces<IEnumerable<AdministrativeAreaDto>>();

        group.MapGet("/provinces/{provincePsgcCode}/municipalities", GetMunicipalitiesByProvince)
            .WithName("GetMunicipalitiesByProvince")
            .WithSummary("Get all municipalities under a province")
            .Produces<IEnumerable<AdministrativeAreaDto>>();

        // Barangays (children of city or municipality)
        group.MapGet("/barangays", GetBarangays)
            .WithName("GetBarangays")
            .WithSummary("Get all barangays")
            .Produces<IEnumerable<AdministrativeAreaDto>>();

        group.MapGet("/localities/{cityOrMunicipalityPsgcCode}/barangays", GetBarangaysByCityOrMunicipality)
            .WithName("GetBarangaysByCityOrMunicipality")
            .WithSummary("Get all barangays under a given city or municipality")
            .Produces<IEnumerable<AdministrativeAreaDto>>();

        return app;
    }

    private static async Task<IResult> GetRegions(IAdministrativeAreaService service, CancellationToken ct)
    {
        var items = await service.GetRegionsAsync(ct);
        return Results.Ok(items);
    }

    private static async Task<IResult> GetProvinces(IAdministrativeAreaService service, CancellationToken ct)
    {
        var items = await service.GetProvincesAsync(ct);
        return Results.Ok(items);
    }

    private static async Task<IResult> GetProvincesByRegion(string regionPsgcCode, IAdministrativeAreaService service, CancellationToken ct)
    {
        var items = await service.GetProvincesByRegionAsync(regionPsgcCode, ct);
        return Results.Ok(items);
    }

    private static async Task<IResult> GetCities(IAdministrativeAreaService service, CancellationToken ct)
    {
        var items = await service.GetCitiesAsync(ct);
        return Results.Ok(items);
    }

    private static async Task<IResult> GetCitiesByProvince(string provincePsgcCode, IAdministrativeAreaService service, CancellationToken ct)
    {
        var items = await service.GetCitiesByProvinceAsync(provincePsgcCode, ct);
        return Results.Ok(items);
    }

    private static async Task<IResult> GetMunicipalities(IAdministrativeAreaService service, CancellationToken ct)
    {
        var items = await service.GetMunicipalitiesAsync(ct);
        return Results.Ok(items);
    }

    private static async Task<IResult> GetMunicipalitiesByProvince(string provincePsgcCode, IAdministrativeAreaService service, CancellationToken ct)
    {
        var items = await service.GetMunicipalitiesByProvinceAsync(provincePsgcCode, ct);
        return Results.Ok(items);
    }

    private static async Task<IResult> GetBarangays(IAdministrativeAreaService service, CancellationToken ct)
    {
        var items = await service.GetBarangaysAsync(ct);
        return Results.Ok(items);
    }

    private static async Task<IResult> GetBarangaysByCityOrMunicipality(string cityOrMunicipalityPsgcCode, IAdministrativeAreaService service, CancellationToken ct)
    {
        var items = await service.GetBarangaysByCityOrMunicipalityAsync(cityOrMunicipalityPsgcCode, ct);
        return Results.Ok(items);
    }
}




