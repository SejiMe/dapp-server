using dengue.watch.api.features.weatherpooling.services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace dengue.watch.api.features.weatherpooling.endpoints;

public class FetchBarangayHistoricalWeatherByDate : IEndpoint
{
    public static IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("weatherpooling")
            .WithTags("Weather Pooling")
            .WithSummary("Fetch historical weather for a single barangay by date");

        group.MapPost("fetch-one", HandleAsync)
            .WithName("FetchBarangayHistoricalWeatherByDate")
            .Produces<Ok<Response>>()
            .Produces<BadRequest<string>>()
            .Produces<NotFound<string>>();

        return group;
    }

    public record Request(string PsgcCode, DateTime Date);

    public record Response(
        string PsgcCode,
        DateTime Date,
        bool Inserted,
        bool AlreadyExists,
        string? Message);

    private static async Task<Results<Ok<Response>, BadRequest<string>, NotFound<string>>> HandleAsync(
        [FromBody] Request request,
        [FromServices] IWeatherDataAPI weatherDataApi,
        [FromServices] WeatherDataProcessor processor,
        [FromServices] ApplicationDbContext db,
        [FromServices] ILogger<FetchBarangayHistoricalWeatherByDate> logger,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.PsgcCode) || request.PsgcCode.Length != 10)
            return TypedResults.BadRequest("PsgcCode must be exactly 10 characters.");

        // Normalize input date to DateOnly to align with OpenMeteo archive daily response
        var dateOnly = DateOnly.FromDateTime(request.Date);

        // Lookup barangay coordinates
        var area = await db.AdministrativeAreas
            .AsNoTracking()
            .Where(a => a.PsgcCode == request.PsgcCode)
            .Select(a => new { a.PsgcCode, a.GeographicLevel, a.Latitude, a.Longitude })
            .FirstOrDefaultAsync(cancellationToken);

        if (area is null)
            return TypedResults.NotFound($"Administrative area with PSGC {request.PsgcCode} was not found.");

        if (!string.Equals(area.GeographicLevel, "Bgy", StringComparison.OrdinalIgnoreCase))
            return TypedResults.BadRequest($"PSGC {request.PsgcCode} is not a barangay (GeographicLevel={area.GeographicLevel}).");

        if (!area.Latitude.HasValue || !area.Longitude.HasValue)
            return TypedResults.BadRequest($"Barangay {request.PsgcCode} has no coordinates (Latitude/Longitude). ");

        try
        {
            var apiResponse = await weatherDataApi.GetHistoricalDataAsync(
                (decimal)area.Latitude.Value,
                (decimal)area.Longitude.Value,
                cancellationToken,
                dateOnly);

            var dayData = processor.Get1DayData(apiResponse);
            dayData.FK_PsgcCode = request.PsgcCode;

            // Ensure we store UTC DateTime (DailyWeather.Date is mapped to timestamptz)
            var dateToPersist = DateTime.SpecifyKind(dayData.Date, DateTimeKind.Utc);

            var exists = await db.DailyWeather
                .AnyAsync(x => x.Date == dateToPersist && x.PsgcCode == request.PsgcCode, cancellationToken);

            if (exists)
            {
                return TypedResults.Ok(new Response(
                    PsgcCode: request.PsgcCode,
                    Date: dateToPersist,
                    Inserted: false,
                    AlreadyExists: true,
                    Message: "Daily weather already exists for this PSGC and date."));
            }

            var entity = new DailyWeather
            {
                Date = dateToPersist,
                PsgcCode = request.PsgcCode,
                WeatherCodeId = dayData.WeatherCode,
                Temperature = (float)dayData.TemperatureMean,
                Precipitation = (float)dayData.PrecipitationSum,
                Humidity = (float)dayData.RelativeHumidityMean
            };

            db.DailyWeather.Add(entity);
            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Inserted DailyWeather for {Psgc} at {Date}", request.PsgcCode, dateToPersist);

            return TypedResults.Ok(new Response(
                PsgcCode: request.PsgcCode,
                Date: dateToPersist,
                Inserted: true,
                AlreadyExists: false,
                Message: "Inserted daily weather data."));
        }
        catch (ValidationException ve)
        {
            return TypedResults.BadRequest(ve.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed fetching/inserting weather data for {Psgc} at {Date}", request.PsgcCode, request.Date);
            return TypedResults.BadRequest($"Failed to fetch or insert weather data: {ex.Message}");
        }
    }
}
