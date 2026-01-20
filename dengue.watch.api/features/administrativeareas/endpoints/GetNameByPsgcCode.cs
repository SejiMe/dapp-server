using System.Text.RegularExpressions;
using dengue.watch.api.infrastructure.database;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace dengue.watch.api.features.administrativeareas.endpoints;

/// <summary>
/// Endpoint to get administrative area details by PSGC code
/// </summary>
public partial class GetNameByPsgcCode : IEndpoint
{
    // PSA PSGC code pattern: exactly 10 numeric digits
    // Format: RRPPCCMBBB where:
    // RR = Region (2 digits)
    // PP = Province (2 digits)
    // CC = City/Municipality (2 digits)
    // M = District (1 digit) - usually 0 for non-NCR
    // BBB = Barangay (3 digits)
    [GeneratedRegex(@"^\d{10}$", RegexOptions.Compiled)]
    private static partial Regex PsgcCodePattern();

    public static IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("administrative-areas")
            .WithTags("Administrative Areas")
            .WithOpenApi();

        group.MapGet("/{psgcCode}", Handler)
            .WithName("GetNameByPsgcCode")
            .WithSummary("Get administrative area details by PSGC code")
            .WithDescription("Retrieves administrative area information using the Philippine Standard Geographic Code (PSGC). " +
                           "PSGC codes follow PSA standard format: 10 numeric digits (RRPPCCMBBB).")
            .Produces<Ok<AdministrativeAreaDetailDto>>()
            .Produces<NotFound<string>>()
            .Produces<BadRequest<string>>();

        return group;
    }

    private static async Task<Results<Ok<AdministrativeAreaDetailDto>, NotFound<string>, BadRequest<string>>> Handler(
        string psgcCode,
        [FromServices] ApplicationDbContext db,
        CancellationToken ct = default)
    {
        // Validate PSGC code format
        if (string.IsNullOrWhiteSpace(psgcCode))
        {
            return TypedResults.BadRequest("PSGC code is required");
        }

        // Trim whitespace
        psgcCode = psgcCode.Trim();

        // Validate PSA PSGC pattern (10 numeric digits)
        if (!PsgcCodePattern().IsMatch(psgcCode))
        {
            return TypedResults.BadRequest(
                "Invalid PSGC code format. PSGC codes must be exactly 10 numeric digits following PSA standard (RRPPCCMBBB).");
        }

        // Query the administrative area
        var area = await db.AdministrativeAreas
            .AsNoTracking()
            .Where(a => a.PsgcCode == psgcCode)
            .Select(a => new AdministrativeAreaDetailDto(
                a.PsgcCode,
                a.Name,
                a.GeographicLevel,
                a.OldNames,
                a.Latitude,
                a.Longitude
            ))
            .FirstOrDefaultAsync(ct);

        if (area is null)
        {
            return TypedResults.NotFound($"Administrative area with PSGC code '{psgcCode}' was not found");
        }

        return TypedResults.Ok(area);
    }
}

/// <summary>
/// Detailed DTO for administrative area including old names
/// </summary>
public record AdministrativeAreaDetailDto(
    string PsgcCode,
    string Name,
    string GeographicLevel,
    string? OldNames,
    decimal? Latitude,
    decimal? Longitude
);
