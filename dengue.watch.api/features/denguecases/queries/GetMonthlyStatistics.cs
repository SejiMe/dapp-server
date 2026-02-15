using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace dengue.watch.api.features.denguecases.queries;

/// <summary>
/// Get monthly average statistics
/// </summary>
public class GetMonthlyStatistics : IEndpoint
{
    public static IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("dengue-cases")
            .WithSummary("Monthly Statistics")
            .WithTags("Dengue Cases - Analytics");

        // Average predicted cases per month
        group.MapGet("monthly-averages/predicted", GetAveragePredictions.Handler)
            .Produces<MonthlyAverageResponse>(200);

        // Average actual cases per month
        group.MapGet("monthly-averages/actual", GetAverageCases.Handler)
            .Produces<MonthlyAverageResponse>(200);

        // Combined comparison
        group.MapGet("monthly-averages/comparison", GetComparison.Handler)
            .Produces<MonthlyComparisonResponse>(200);

        return group;
    }
}

public record MonthlyAverageResponse(
    string? PsgcCode,
    int? Year,
    List<MonthlyAverageItem> MonthlyAverages
);

public record MonthlyAverageItem(
    int Month,
    string MonthName,
    double AverageValue
);

public record MonthlyComparisonResponse(
    string? PsgcCode,
    int? Year,
    List<MonthlyComparisonItem> Data
);

public record MonthlyComparisonItem(
    int Month,
    string MonthName,
    double AveragePredicted,
    double AverageActual
);

/// <summary>
/// Get average predicted cases per month
/// </summary>
public class GetAveragePredictions : IEndpoint
{
    public static IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder app)
    {
        return app;
    }

    public static async Task<Ok<MonthlyAverageResponse>> Handler(
        [FromServices] ApplicationDbContext _db,
        string? psgcCode = null,
        int? year = null,
        CancellationToken cancellationToken = default)
    {
        var query = _db.PredictedWeeklyDengues.AsQueryable();

        if (!string.IsNullOrEmpty(psgcCode))
        {
            query = query.Where(p => p.PsgcCode == psgcCode);
        }

        if (year.HasValue)
        {
            query = query.Where(p => p.PredictedIsoYear == year.Value);
        }

        var monthlyData = await query
            .GroupBy(p => new { p.PredictedIsoYear, Month = GetMonthFromWeek(p.PredictedIsoWeek, p.PredictedIsoYear) })
            .Select(g => new
            {
                g.Key.Month,
                g.Key.PredictedIsoYear,
                Average = g.Average(p => p.PredictedValue)
            })
            .ToListAsync(cancellationToken);

        // Group by month and calculate overall average
        var result = monthlyData
            .GroupBy(m => m.Month)
            .Select(g => new MonthlyAverageItem(
                g.Key,
                GetMonthName(g.Key),
                Math.Round(g.Average(m => m.Average), 2)
            ))
            .OrderBy(m => m.Month)
            .ToList();

        return TypedResults.Ok(new MonthlyAverageResponse(psgcCode, year, result));
    }

    private static int GetMonthFromWeek(int isoWeek, int year)
    {
        var jan1 = new DateTime(year, 1, 1);
        var daysOffset = DayOfWeek.Thursday - jan1.DayOfWeek;
        var firstThursday = jan1.AddDays(daysOffset);
        var cal = System.Globalization.CultureInfo.CurrentCulture.Calendar;
        var firstWeek = cal.GetWeekOfYear(firstThursday, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        var weekOffset = firstWeek <= 1 ? 0 : (firstWeek - 1) * 7;
        return cal.GetMonth(firstThursday.AddDays(weekOffset + (isoWeek - 1) * 7));
    }

    private static string GetMonthName(int month)
    {
        return new DateTime(2000, month, 1).ToString("MMMM");
    }
}

/// <summary>
/// Get average actual cases per month
/// </summary>
public class GetAverageCases : IEndpoint
{
    public static IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder app)
    {
        return app;
    }

    public static async Task<Ok<MonthlyAverageResponse>> Handler(
        [FromServices] ApplicationDbContext _db,
        string? psgcCode = null,
        int? year = null,
        CancellationToken cancellationToken = default)
    {
        var query = _db.MonthlyDengueCases.AsQueryable();

        if (!string.IsNullOrEmpty(psgcCode))
        {
            query = query.Where(m => m.PsgcCode == psgcCode);
        }

        if (year.HasValue)
        {
            query = query.Where(m => m.Year == year.Value);
        }

        var result = await query
            .GroupBy(m => m.MonthNumber)
            .Select(g => new MonthlyAverageItem(
                g.Key,
                GetMonthName(g.Key),
                Math.Round(g.Average(m => m.CaseCount), 2)
            ))
            .OrderBy(m => m.Month)
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(new MonthlyAverageResponse(psgcCode, year, result));
    }

    private static string GetMonthName(int month)
    {
        return new DateTime(2000, month, 1).ToString("MMMM");
    }
}

/// <summary>
/// Get comparison of predicted vs actual cases
/// </summary>
public class GetComparison : IEndpoint
{
    public static IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder app)
    {
        return app;
    }

    public static async Task<Ok<MonthlyComparisonResponse>> Handler(
        [FromServices] ApplicationDbContext _db,
        string? psgcCode = null,
        int? year = null,
        CancellationToken cancellationToken = default)
    {
        // Get predicted
        var predictedQuery = _db.PredictedWeeklyDengues.AsQueryable();
        if (!string.IsNullOrEmpty(psgcCode))
        {
            predictedQuery = predictedQuery.Where(p => p.PsgcCode == psgcCode);
        }
        if (year.HasValue)
        {
            predictedQuery = predictedQuery.Where(p => p.PredictedIsoYear == year.Value);
        }

        var predicted = await predictedQuery
            .GroupBy(p => GetMonthFromWeek(p.PredictedIsoWeek, p.PredictedIsoYear))
            .Select(g => new { Month = g.Key, Average = g.Average(p => p.PredictedValue) })
            .ToListAsync(cancellationToken);

        // Get actual
        var actualQuery = _db.MonthlyDengueCases.AsQueryable();
        if (!string.IsNullOrEmpty(psgcCode))
        {
            actualQuery = actualQuery.Where(m => m.PsgcCode == psgcCode);
        }
        if (year.HasValue)
        {
            actualQuery = actualQuery.Where(m => m.Year == year.Value);
        }

        var actual = await actualQuery
            .GroupBy(m => m.MonthNumber)
            .Select(g => new { Month = g.Key, Average = g.Average(m => m.CaseCount) })
            .ToListAsync(cancellationToken);

        // Merge data
        var allMonths = predicted.Select(p => p.Month).Union(actual.Select(a => a.Month)).Distinct().OrderBy(m => m);

        var result = allMonths.Select(month => new MonthlyComparisonItem(
            month,
            GetMonthName(month),
            Math.Round(predicted.FirstOrDefault(p => p.Month == month)?.Average ?? 0, 2),
            Math.Round(actual.FirstOrDefault(a => a.Month == month)?.Average ?? 0, 2)
        )).ToList();

        return TypedResults.Ok(new MonthlyComparisonResponse(psgcCode, year, result));
    }

    private static int GetMonthFromWeek(int isoWeek, int year)
    {
        var jan1 = new DateTime(year, 1, 1);
        var daysOffset = DayOfWeek.Thursday - jan1.DayOfWeek;
        var firstThursday = jan1.AddDays(daysOffset);
        var cal = System.Globalization.CultureInfo.CurrentCulture.Calendar;
        var firstWeek = cal.GetWeekOfYear(firstThursday, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        var weekOffset = firstWeek <= 1 ? 0 : (firstWeek - 1) * 7;
        return cal.GetMonth(firstThursday.AddDays(weekOffset + (isoWeek - 1) * 7));
    }

    private static string GetMonthName(int month)
    {
        return new DateTime(2000, month, 1).ToString("MMMM");
    }
}
