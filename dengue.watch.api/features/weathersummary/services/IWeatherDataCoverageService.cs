namespace dengue.watch.api.features.weathersummary.services;

public interface IWeatherDataCoverageService
{
    /// <summary>
    /// Gets weather data coverage for a single PSGC code within a date range.
    /// </summary>
    Task<WeatherDataCoverageResult> GetCoverageAsync(
        string psgcCode,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets weather data coverage for all barangays within a date range.
    /// </summary>
    Task<BulkWeatherDataCoverageResult> GetBulkCoverageAsync(
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default);
}

public record WeatherDataCoverageResult(
    string PsgcCode,
    DateOnly StartDate,
    DateOnly EndDate,
    int TotalExpectedDays,
    int AvailableDays,
    int MissingDays,
    double CoveragePercentage,
    List<DateOnly> DatesWithData,
    List<DateOnly> MissingDates);

public record BulkWeatherDataCoverageResult(
    DateOnly StartDate,
    DateOnly EndDate,
    int TotalBarangays,
    int BarangaysWithFullCoverage,
    int BarangaysWithPartialCoverage,
    int BarangaysWithNoCoverage,
    double AverageCoveragePercentage,
    List<WeatherDataCoverageResult> Results);
