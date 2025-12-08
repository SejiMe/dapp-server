using System.Globalization;
using CsvHelper;

namespace dengue.watch.api.features.denguecases.commands;

public class CreateCsvForPrediction : IEndpoint
{
    public static IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("dengue-cases")
            .WithTags("Dengue Cases")
            .WithSummary("Create a CSV for Prediction in N8n");

        group.MapPost("create-bulk", Handler);
        return group;
    }
    public record CreateCSVRequest(int starting_week, int last_week, int[] years);


    private bool IsAValidYear(int year) => year > 2014;
    private static async Task<IResult> Handler([FromBody] CreateCSVRequest request, [FromServices] ApplicationDbContext db)
    {
        try
        {
            Calendar calendar = new GregorianCalendar();
            
            DateTime[] dateTimes =
                request.years
                    .Select((y) => new DateTime(y, 1, 1))
                    .ToArray();
            
            var Years = dateTimes.Select(p => calendar.GetYear(p)).ToArray();
            var psgcCodes = await db.AdministrativeAreas.Where(p => p.GeographicLevel.ToLower() == "bgy").Select(p => p.PsgcCode).ToListAsync();
            List<DateForExtract> dates = [];
            
            if (!psgcCodes.Any())
            {
                return Results.Problem("No barangays found in database");
            }
            
            foreach (string code in psgcCodes)
            {
                
                foreach (int year in Years)
                {

                    var weeks = request.last_week;
                    // ISOWeek.GetWeeksInYear(year);
                    int element = 0;
                    int week = request.starting_week;

                    while (week <= weeks)
                    {
                        var res = DateOnly.FromDateTime(ISOWeek.ToDateTime(year, week, DayOfWeek.Monday));
                        dates.Add(new(code,year,week,res.ToString("yyyy-MM-dd")));
                        week++;
                        element++;
                    }
                }
            }
            using var memoryStream = new MemoryStream();
            using var writer = new StreamWriter(
                memoryStream
            );

            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        
            await csv.WriteRecordsAsync(dates);
            await writer.FlushAsync();

            memoryStream.Position = 0;
            
            return Results.File(memoryStream.ToArray(), "text/csv", "DatesForExtract.csv"); 
        }
        catch (Exception e)
        {
            return Results.Problem($"Error generating CSV: {e.Message}");
        }
    }
}

public record DateForExtract(string psgcCode, int year, int week, string date);