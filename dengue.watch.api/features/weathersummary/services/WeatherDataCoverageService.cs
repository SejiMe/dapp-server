using dengue.watch.api.infrastructure.database;
using Microsoft.EntityFrameworkCore;

namespace dengue.watch.api.features.weathersummary.services;

public class WeatherDataCoverageService : IWeatherDataCoverageService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<WeatherDataCoverageService> _logger;

    public WeatherDataCoverageService(
        ApplicationDbContext dbContext,
        ILogger<WeatherDataCoverageService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<WeatherDataCoverageResult> GetCoverageAsync(
        string psgcCode,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        var totalDays = endDate.DayNumber - startDate.DayNumber + 1;
        
        // Create UTC DateTime boundaries for Npgsql timestamptz compatibility
        var startDateTime = new DateTime(startDate.Year, startDate.Month, startDate.Day, 0, 0, 0, DateTimeKind.Utc);
        var endDateTime = new DateTime(endDate.Year, endDate.Month, endDate.Day, 23, 59, 59, 999, DateTimeKind.Utc);

        var datesWithData = await _dbContext.DailyWeather
            .AsNoTracking()
            .Where(w => w.PsgcCode == psgcCode
                     && w.Date >= startDateTime
                     && w.Date <= endDateTime)
            .Select(w => w.Date.Date)
            .Distinct()
            .OrderBy(d => d)
            .ToListAsync(cancellationToken);

        // Convert to DateOnly on client side
        var datesOnly = datesWithData.Select(d => DateOnly.FromDateTime(d)).ToList();

        var allExpectedDates = Enumerable
            .Range(0, totalDays)
            .Select(offset => startDate.AddDays(offset))
            .ToList();

        var datesWithDataSet = datesOnly.ToHashSet();
        var missingDates = allExpectedDates
            .Where(date => !datesWithDataSet.Contains(date))
            .ToList();

        var coveragePercentage = totalDays > 0
            ? Math.Round((double)datesOnly.Count / totalDays * 100, 2)
            : 0;

        return new WeatherDataCoverageResult(
            PsgcCode: psgcCode,
            StartDate: startDate,
            EndDate: endDate,
            TotalExpectedDays: totalDays,
            AvailableDays: datesOnly.Count,
            MissingDays: missingDates.Count,
            CoveragePercentage: coveragePercentage,
            DatesWithData: datesOnly,
            MissingDates: missingDates);
    }

    public async Task<BulkWeatherDataCoverageResult> GetBulkCoverageAsync(
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        var totalDays = endDate.DayNumber - startDate.DayNumber + 1;
        
        // Create UTC DateTime boundaries for Npgsql timestamptz compatibility
        var startDateTime = new DateTime(startDate.Year, startDate.Month, startDate.Day, 0, 0, 0, DateTimeKind.Utc);
        var endDateTime = new DateTime(endDate.Year, endDate.Month, endDate.Day, 23, 59, 59, 999, DateTimeKind.Utc);

        // Get all barangays with coordinates
        var barangays = await _dbContext.AdministrativeAreas
            .AsNoTracking()
            .Where(a => a.GeographicLevel == "Bgy" && a.Latitude.HasValue && a.Longitude.HasValue)
            .Select(a => a.PsgcCode)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Processing weather data coverage for {Count} barangays from {Start} to {End}",
            barangays.Count, startDate, endDate);

        // Fetch all weather data dates for the range - get distinct psgc/date pairs first
        var weatherDataRaw = await _dbContext.DailyWeather
            .AsNoTracking()
            .Where(w => w.Date >= startDateTime && w.Date <= endDateTime)
            .Select(w => new { w.PsgcCode, DateValue = w.Date.Date })
            .Distinct()
            .ToListAsync(cancellationToken);

        // Group by PsgcCode on the client side and convert to DateOnly
        var weatherDataByPsgc = weatherDataRaw
            .GroupBy(w => w.PsgcCode)
            .ToDictionary(
                g => g.Key,
                g => g.Select(w => DateOnly.FromDateTime(w.DateValue)).Distinct().ToList());

        var allExpectedDates = Enumerable
            .Range(0, totalDays)
            .Select(offset => startDate.AddDays(offset))
            .ToList();

        var results = new List<WeatherDataCoverageResult>();
        int fullCoverage = 0;
        int partialCoverage = 0;
        int noCoverage = 0;
        double totalCoveragePercentage = 0;

        foreach (var psgcCode in barangays)
        {
            var datesWithData = weatherDataByPsgc.TryGetValue(psgcCode, out var dates)
                ? dates.OrderBy(d => d).ToList()
                : [];

            var datesWithDataSet = datesWithData.ToHashSet();
            var missingDates = allExpectedDates
                .Where(date => !datesWithDataSet.Contains(date))
                .ToList();

            var coveragePercentage = totalDays > 0
                ? Math.Round((double)datesWithData.Count / totalDays * 100, 2)
                : 0;

            totalCoveragePercentage += coveragePercentage;

            if (coveragePercentage >= 100)
                fullCoverage++;
            else if (coveragePercentage > 0)
                partialCoverage++;
            else
                noCoverage++;

            results.Add(new WeatherDataCoverageResult(
                PsgcCode: psgcCode,
                StartDate: startDate,
                EndDate: endDate,
                TotalExpectedDays: totalDays,
                AvailableDays: datesWithData.Count,
                MissingDays: missingDates.Count,
                CoveragePercentage: coveragePercentage,
                DatesWithData: datesWithData,
                MissingDates: missingDates));
        }

        var averageCoverage = barangays.Count > 0
            ? Math.Round(totalCoveragePercentage / barangays.Count, 2)
            : 0;

        _logger.LogInformation(
            "Bulk coverage complete: Full={Full}, Partial={Partial}, None={None}, Average={Avg}%",
            fullCoverage, partialCoverage, noCoverage, averageCoverage);

        return new BulkWeatherDataCoverageResult(
            StartDate: startDate,
            EndDate: endDate,
            TotalBarangays: barangays.Count,
            BarangaysWithFullCoverage: fullCoverage,
            BarangaysWithPartialCoverage: partialCoverage,
            BarangaysWithNoCoverage: noCoverage,
            AverageCoveragePercentage: averageCoverage,
            Results: results);
    }
}
