using System.Data;
using System.Globalization;
using System.Text;
using dengue.watch.api.features.trainingdatapipeline.models;
using dengue.watch.api.features.trainingdatapipeline.services;
using Npgsql;
using NpgsqlTypes;

namespace dengue.watch.api.common.repositories;
public interface IAggregatedWeeklyHistoricalWeatherRepository
{
    Task<WeeklyTrainingWeatherResult> GetWeeklySnapshotsAsync(
        string psgcCode,
        IReadOnlyCollection<int> dengueYears,
        int? dengueWeekNumber,
        (int From, int To)? dengueWeekRange,
        CancellationToken cancellationToken = default);

    Task<AggregatedWeeklyHistoricalWeatherSnapshot> GetWeeklyHistoricalWeatherSnapshotAsync(
        string psgc,
        int year,
        int isoweek,
        CancellationToken cancellationToken = default);

}

public class AggregatedWeeklyHistoricalWeatherRepository : IAggregatedWeeklyHistoricalWeatherRepository
{
    //
    private const string BaseSelectSql = "SELECT " +
        "we.main_description AS \"WeatherMainDescription\", " +
        "a.weather_code_id AS \"WeatherCodeId\", " +
        "CAST(DATE_PART('week', a.date) AS INT) AS \"WeekNumber\", " +
        "CAST(DATE_PART('isoyear', a.date) AS INT) AS \"Year\", " +
        "a.date AS \"Date\", " +
        "a.temperature AS \"Temperature\", " +
        "a.precipitation AS \"Precipitation\", " +
        "a.humidity AS \"Humidity\" " +
        "FROM public.daily_weather AS a " +
        "LEFT JOIN weather_codes AS we ON we.id = a.weather_code_id " +
        "WHERE a.psgc_code = @psgc_code " +
        "AND DATE_PART('isoyear', a.date) = ANY(@years) ";


    private const string BaseSelectWeatherData = @"
        SELECT 
          we.main_description as WeatherMainDescription,
          a.weather_code_id as WeatherCodeId,
          CAST(DATE_PART('week', a.date) AS INT) as WeekNumber,
          CAST(DATE_PART('isoyear', a.date) AS INT) as Year,
          a.date as Date,
          a.temperature as Temperature,
          a.precipitation as Precipitation,
          a.humidity as Humidity
        FROM public.daily_weather as a 
          LEFT JOIN weather_codes as we ON we.id = a.weather_code_id
        where 1=1
        AND a.psgc_code =  @psgc
        AND DATE_PART('week', a.date) = @iso_week
        AND DATE_PART('isoyear', a.date) = @iso_year
        ORDER by date"
    ;
    
    private readonly ApplicationDbContext _dbContext;
    private readonly IWeeklyDataStatisticsService _weeklyStatisticsService;
    private readonly ILogger<AggregatedWeeklyHistoricalWeatherRepository> _logger;
    private readonly SemaphoreSlim _commandSemaphore = new(1, 1);
    private readonly IServiceScopeFactory _serviceScopeFactory;
    
    
    private static readonly TextInfo TitleCaseTextInfo = CultureInfo.InvariantCulture.TextInfo;

    private static readonly string[] HighPriorityWeatherTerms =
    {
        "rain",
        "thunderstorm",
        "storm"
    };

    private static readonly string[] SecondaryPriorityWeatherTerms =
    {
        "drizzle",
        "shower"
    };

    private const double WetWeekTotalPrecipitationThreshold = 50d;
    private const int WetWeekDrizzleDayThreshold = 4;

    public AggregatedWeeklyHistoricalWeatherRepository(
        ApplicationDbContext dbContext,
        IServiceScopeFactory scopeFactory,
        IWeeklyDataStatisticsService weeklyStatisticsService,
        ILogger<AggregatedWeeklyHistoricalWeatherRepository> logger)
    {
        _dbContext = dbContext;
        _serviceScopeFactory = scopeFactory;
        _weeklyStatisticsService = weeklyStatisticsService;
        _logger = logger;
    }

    public async Task<WeeklyTrainingWeatherResult> GetWeeklySnapshotsAsync(
        string psgcCode,
        IReadOnlyCollection<int> dengueYears,
        int? dengueWeekNumber,
        (int From, int To)? dengueWeekRange,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(psgcCode))
        {
            _logger.LogWarning("PSGC code is required when requesting weekly weather snapshots.");
            throw new ValidationException("PSGC code must be provided.");
        }

        if (dengueYears == null || dengueYears.Count == 0)
        {
            _logger.LogWarning("No dengue years provided for PSGC {PsgcCode}.", psgcCode);
            throw new ValidationException("At least one dengue year must be provided.");
        }

        if (dengueWeekNumber.HasValue && dengueWeekRange.HasValue)
        {
            _logger.LogWarning("Both dengueWeekNumber and dengueWeekRange were provided for PSGC {PsgcCode}.", psgcCode);
            throw new ValidationException("Specify either a single dengueWeekNumber or a dengueWeekRange, not both.");
        }

        if (dengueWeekNumber.HasValue && (dengueWeekNumber.Value < 1 || dengueWeekNumber.Value > 53))
        {
            _logger.LogWarning("Invalid dengue week number {WeekNumber} provided for PSGC {PsgcCode}.", dengueWeekNumber.Value, psgcCode);
            throw new ValidationException("Dengue week number must be between 1 and 53.");
        }

        if (dengueWeekRange.HasValue)
        {
            var (fromWeek, toWeek) = dengueWeekRange.Value;

            if (fromWeek < 1 || toWeek < 1 || fromWeek > 53 || toWeek > 53)
            {
                _logger.LogWarning("Dengue week range {WeekFrom}-{WeekTo} is out of bounds for PSGC {PsgcCode}.", fromWeek, toWeek, psgcCode);
                throw new ValidationException("Dengue week range boundaries must be between 1 and 53.");
            }

            if (fromWeek > toWeek)
            {
                _logger.LogWarning("Dengue week range start {WeekFrom} is greater than end {WeekTo} for PSGC {PsgcCode}.", fromWeek, toWeek, psgcCode);
                throw new ValidationException("Dengue week range start must be less than or equal to the end.");
            }
        }

        try
        {
            var dengueRecords = await QueryDengueCasesAsync(
                psgcCode,
                dengueYears,
                dengueWeekNumber,
                dengueWeekRange,
                cancellationToken);

            if (dengueRecords.Count == 0)
            {
                return new WeeklyTrainingWeatherResult(
                    new List<WeeklyTrainingWeatherSnapshot>(),
                    new () { psgcCode });
            }

            var laggedWeather = await FetchLaggedWeatherRowsAsync(
                psgcCode,
                dengueRecords,
                cancellationToken);

            var groupedWeather = laggedWeather
                .GroupBy(row => (row.Year, row.WeekNumber))
                .ToDictionary(group => group.Key, group => group.ToList());

            var snapshots = new List<WeeklyTrainingWeatherSnapshot>();
            var missingWeatherWeeks = new List<string>();

            foreach (var dengue in dengueRecords.OrderBy(record => record.Year).ThenBy(record => record.WeekNumber))
            {
                var lag = CalculateLagWeek(dengue.Year, dengue.WeekNumber);

                if (!groupedWeather.TryGetValue(lag, out var laggedRecords) || laggedRecords.Count < 7)
                {
                    missingWeatherWeeks.Add($"{lag.Year:D4}-W{lag.WeekNumber:D2}");
                    continue;
                }

                var orderedRecords = laggedRecords
                    .OrderBy(row => row.Date)
                    .ToList();

                var temperatureValues = orderedRecords.Select(row => row.Temperature).ToArray();
                var humidityValues = orderedRecords.Select(row => row.Humidity).ToArray();
                var precipitationValues = orderedRecords.Select(row => row.Precipitation).ToArray();
                var weatherCodeValues = orderedRecords.Select(row => row.WeatherCodeId).ToArray();
                var weatherDescriptions = orderedRecords.Select(row => row.WeatherMainDescription ?? string.Empty).ToArray();

                var temperatureStats = _weeklyStatisticsService.CalculateTemperatureStatistics(temperatureValues, temperatureValues);
                var humidityStats = _weeklyStatisticsService.CalculateHumidityStatistics(humidityValues, humidityValues);
                var precipitationStats = _weeklyStatisticsService.CalculatePrecipitationStatistics(precipitationValues, precipitationValues);

                var weatherCodeFrequency = weatherCodeValues
                    .GroupBy(code => code)
                    .Select(grouping => new
                    {
                        Code = grouping.Key,
                        Count = grouping.Count()
                    })
                    .OrderByDescending(entry => entry.Count)
                    .ThenBy(entry => entry.Code)
                    .FirstOrDefault();

                var mostCommonWeatherCodeId = weatherCodeFrequency?.Code ?? 0;
                var occurrenceCount = weatherCodeFrequency?.Count ?? 0;

                var dominance = DetermineDominantWeatherCategory(weatherDescriptions);
                var mostCommonDescription = dominance.Description ?? GetMostCommonDescription(weatherDescriptions);

                var dominantWeatherCategory = dominance.HasPriorityMatch
                    ? dominance.Category
                    : mostCommonDescription ?? dominance.Category;

                dominantWeatherCategory ??= "Unclassified";

                var isWetWeek = IsWetWeek(precipitationValues, weatherDescriptions);

                var weekStartDate = DateOnly.FromDateTime(ISOWeek.ToDateTime(lag.Year, lag.WeekNumber, DayOfWeek.Monday));

                snapshots.Add(new WeeklyTrainingWeatherSnapshot(
                    PsgcCode: psgcCode,
                    DengueYear: dengue.Year,
                    DengueWeekNumber: dengue.WeekNumber,
                    DengueCaseCount: dengue.CaseCount,
                    LagYear: lag.Year,
                    LagWeekNumber: lag.WeekNumber,
                    LagWeekStartDate: weekStartDate,
                    Temperature: temperatureStats,
                    Humidity: humidityStats,
                    Precipitation: precipitationStats,
                    MostCommonWeatherCodeId: mostCommonWeatherCodeId,
                    MostCommonWeatherDescription: mostCommonDescription,
                    OccurrenceCount: occurrenceCount,
                    DominantWeatherCategory: dominantWeatherCategory,
                    IsWetWeek: isWetWeek));
            }

        return new WeeklyTrainingWeatherResult(
            snapshots,
            missingWeatherWeeks);
        }
        catch (PostgresException ex)
        {
            _logger.LogError(
                ex,
                "Database error while retrieving weekly weather snapshots for PSGC {PsgcCode}.",
                psgcCode);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error while retrieving weekly weather snapshots for PSGC {PsgcCode}.",
                psgcCode);
            throw;
        }
    }

    // Currently Working
    public async Task<AggregatedWeeklyHistoricalWeatherSnapshot> GetWeeklyHistoricalWeatherSnapshotAsync(string psgc, int year, int isoweek, CancellationToken cancellationToken = default)
    {
        if (psgc is not { Length: 10 })
            throw new ValidationException("Can't Process this data");

    
        if(isoweek < 1 ||  isoweek > 53)
            throw new ValidationException("Can't Process iso week");
        
        if(year < 2012)
            throw new ValidationException("Can't process years 2012 below");

        List<NpgsqlParameter> parameters = new List<NpgsqlParameter>();
        
        parameters.Add(new NpgsqlParameter("psgc", psgc));
        parameters.Add(new NpgsqlParameter("iso_week", isoweek));
        parameters.Add(new NpgsqlParameter("iso_year", year));
        
        QueryInfo queryInfo = new QueryInfo(BaseSelectWeatherData, parameters);
        
        
        
        try
        {
            
            var weatherDataRecord =  await ExecuteWeatherQueryAsync(queryInfo.Sql, queryInfo.Parameters, cancellationToken);
            
            var temperatureValues = weatherDataRecord.Select(row => row.Temperature).ToArray();
            var humidityValues = weatherDataRecord.Select(row => row.Humidity).ToArray();
            var precipitationValues = weatherDataRecord.Select(row => row.Precipitation).ToArray();
            var weatherCodeValues = weatherDataRecord.Select(row => row.WeatherCodeId).ToArray();
            var weatherDescriptions = weatherDataRecord.Select(row => row.WeatherMainDescription ?? string.Empty).ToArray();

            var temperatureStats = _weeklyStatisticsService.CalculateTemperatureStatistics(temperatureValues, temperatureValues);
            var humidityStats = _weeklyStatisticsService.CalculateHumidityStatistics(humidityValues, humidityValues);
            var precipitationStats = _weeklyStatisticsService.CalculatePrecipitationStatistics(precipitationValues, precipitationValues);
            
            var weatherCodeFrequency = weatherCodeValues
                .GroupBy(code => code)
                .Select(grouping => new
                {
                    Code = grouping.Key,
                    Count = grouping.Count()
                })
                .OrderByDescending(entry => entry.Count)
                .ThenBy(entry => entry.Code)
                .FirstOrDefault();

            var mostCommonWeatherCodeId = weatherCodeFrequency?.Code ?? 0;
            var occurrenceCount = weatherCodeFrequency?.Count ?? 0;

            var dominance = DetermineDominantWeatherCategory(weatherDescriptions);
            var mostCommonDescription = dominance.Description ?? GetMostCommonDescription(weatherDescriptions);

            var dominantWeatherCategory = dominance.HasPriorityMatch
                ? dominance.Category
                : mostCommonDescription ?? dominance.Category;

            dominantWeatherCategory ??= "Unclassified";

            var isWetWeek = IsWetWeek(precipitationValues, weatherDescriptions);
            
            AggregatedWeeklyHistoricalWeatherSnapshot aggregatedData = new(
                psgc,
                year,
                isoweek,
                temperatureStats,
                humidityStats,
                precipitationStats,
                mostCommonWeatherCodeId,
                occurrenceCount,
                mostCommonDescription,
                isWetWeek
                );
            
            
            
            return aggregatedData;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        
       
    }

    private async Task<List<RawWeatherRow>> ExecuteWeatherQueryAsync(
        string sql,
        IReadOnlyList<NpgsqlParameter> parameters,
        CancellationToken cancellationToken)
    {
        await _commandSemaphore.WaitAsync(cancellationToken);

        try
        {
            var rawRows = await _dbContext.Database
                .SqlQueryRaw<RawWeatherRow>(sql, parameters.ToArray())
                .ToListAsync(cancellationToken);

            _logger.LogDebug("Retrieved {RowCount} raw weather rows.", rawRows.Count);

            return rawRows;
        }
        finally
        {
            _commandSemaphore.Release();
        }
    }

    private static (int Year, int WeekNumber) CalculateLagWeek(int dengueYear, int dengueWeek)
    {
        var date = ISOWeek.ToDateTime(dengueYear, dengueWeek, DayOfWeek.Monday);
        var lagDate = date.AddDays(-14);
        var lagYear = ISOWeek.GetYear(lagDate);
        var lagWeek = ISOWeek.GetWeekOfYear(lagDate);
        return (lagYear, lagWeek);
    }

    private async Task<List<RawWeatherRow>> FetchLaggedWeatherRowsAsync(
        string psgcCode,
        IReadOnlyCollection<DengueWeeklyRecord> dengueRecords,
        CancellationToken cancellationToken)
    {
        var lagWeeks = dengueRecords
            .Select(record => CalculateLagWeek(record.Year, record.WeekNumber))
            .Distinct()
            .ToArray();

        if (lagWeeks.Length == 0)
        {
            return new List<RawWeatherRow>();
        }
        
        var lagYears = lagWeeks.Select(lag => lag.Year).Distinct().ToArray();
        // Check lag years if it is a leap year with 53 weeks
        bool IsLeapYear = DateTime.IsLeapYear(lagYears[0]);

        var minWeek = lagWeeks.Min(lag => lag.WeekNumber);
        var maxWeek = lagWeeks.Max(lag => lag.WeekNumber);

        var queryInfo = BuildWeatherQuery(
            psgcCode,
            lagYears,
            lagWeeks.Length == 1 ? lagWeeks[0].WeekNumber : null,
            lagWeeks.Length == 1 ? null : (minWeek, maxWeek));

        return await ExecuteWeatherQueryAsync(queryInfo.Sql, queryInfo.Parameters, cancellationToken);
    }

    private static QueryInfo BuildWeatherQuery(
        string psgcCode,
        IReadOnlyCollection<int> years,
        int? weekNumber,
        (int From, int To)? weekRange)
    {
        var parameters = new List<NpgsqlParameter>
        {
            new("psgc_code", NpgsqlDbType.Varchar) { Value = psgcCode },
            new("years", NpgsqlDbType.Array | NpgsqlDbType.Integer) { Value = years.ToArray() }
        };

        var sqlBuilder = new StringBuilder(BaseSelectSql);

        if (weekNumber.HasValue)
        {
            sqlBuilder.Append("AND DATE_PART('week', a.date) = @week_number ");
            parameters.Add(new NpgsqlParameter("week_number", NpgsqlDbType.Integer) { Value = weekNumber.Value });
        }
        else if (weekRange.HasValue)
        {
            sqlBuilder.Append("AND DATE_PART('week', a.date) BETWEEN @week_from AND @week_to ");
            parameters.Add(new NpgsqlParameter("week_from", NpgsqlDbType.Integer) { Value = weekRange.Value.From });
            parameters.Add(new NpgsqlParameter("week_to", NpgsqlDbType.Integer) { Value = weekRange.Value.To });
        }
        else
        {
            sqlBuilder.Append("AND DATE_PART('week', a.date) BETWEEN 1 AND 53 ");
        }

        sqlBuilder.Append("ORDER BY a.date");

        return new QueryInfo(sqlBuilder.ToString(), parameters);
    }

    private async Task<List<DengueWeeklyRecord>> QueryDengueCasesAsync(
        string psgcCode,
        IReadOnlyCollection<int> dengueYears,
        int? dengueWeekNumber,
        (int From, int To)? dengueWeekRange,
        CancellationToken cancellationToken)
    {
        await _commandSemaphore.WaitAsync(cancellationToken);
        try
        {
            // using var scope = _serviceScopeFactory.CreateScope();
            // var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var dengueCasesQuery = _dbContext.WeeklyDengueCases
                .AsNoTracking()
                .Where(caseRecord => caseRecord.PsgcCode == psgcCode && dengueYears.Contains(caseRecord.Year));

            if (dengueWeekNumber.HasValue)
            {
                dengueCasesQuery = dengueCasesQuery.Where(caseRecord => caseRecord.WeekNumber == dengueWeekNumber.Value);
            }
            else if (dengueWeekRange.HasValue)
            {
                var (fromWeek, toWeek) = dengueWeekRange.Value;
                dengueCasesQuery = dengueCasesQuery.Where(caseRecord => caseRecord.WeekNumber >= fromWeek && caseRecord.WeekNumber <= toWeek);
            }

            return await dengueCasesQuery
                .Select(caseRecord => new DengueWeeklyRecord(caseRecord.Year, caseRecord.WeekNumber, caseRecord.CaseCount))
                .ToListAsync(cancellationToken);
        }
        finally
        {
           _commandSemaphore.Release();
        }
        
    }

    private static DominantWeatherCategoryResult DetermineDominantWeatherCategory(string[] weatherDescriptions)
    {
        var samples = weatherDescriptions
            .Where(description => !string.IsNullOrWhiteSpace(description))
            .Select(description => new DescriptionSample(description.Trim(), description.Trim().ToLowerInvariant()))
            .ToList();

        if (samples.Count == 0)
        {
            return new DominantWeatherCategoryResult("Unclassified", null, false);
        }

        var highPriority = FindByPriorityTerms(samples, HighPriorityWeatherTerms);
        if (highPriority is not null)
        {
            return new DominantWeatherCategoryResult(TitleCaseTextInfo.ToTitleCase(highPriority.MatchedTerm), highPriority.Sample.Original, true);
        }

        var secondaryPriority = FindByPriorityTerms(samples, SecondaryPriorityWeatherTerms);
        if (secondaryPriority is not null)
        {
            return new DominantWeatherCategoryResult(TitleCaseTextInfo.ToTitleCase(secondaryPriority.MatchedTerm), secondaryPriority.Sample.Original, true);
        }

        var mostCommonDescription = GetMostCommonDescription(samples.Select(sample => sample.Original).ToArray());

        return new DominantWeatherCategoryResult(
            mostCommonDescription ?? "Unclassified",
            mostCommonDescription,
            false);
    }

    private static string? GetMostCommonDescription(string[] weatherDescriptions)
    {
        var samples = weatherDescriptions
            .Where(description => !string.IsNullOrWhiteSpace(description))
            .Select(description => new DescriptionSample(description.Trim(), description.Trim().ToLowerInvariant()))
            .ToList();

        if (samples.Count == 0)
        {
            return null;
        }

        return samples
            .GroupBy(sample => sample.Normalized)
            .Select(group => new
            {
                Normalized = group.Key,
                Original = group
                    .Select(sample => sample.Original)
                    .OrderBy(value => value, StringComparer.Ordinal)
                    .First(),
                Count = group.Count()
            })
            .OrderByDescending(entry => entry.Count)
            .ThenBy(entry => entry.Normalized, StringComparer.Ordinal)
            .Select(entry => entry.Original)
            .FirstOrDefault();
    }

    private static bool IsWetWeek(double[] precipitationValues, string[] weatherDescriptions)
    {
        if (precipitationValues == null || precipitationValues.Length == 0)
        {
            return false;
        }

        var totalPrecipitation = precipitationValues.Sum();
        if (totalPrecipitation >= WetWeekTotalPrecipitationThreshold)
        {
            return true;
        }

        var samples = weatherDescriptions
            .Where(description => !string.IsNullOrWhiteSpace(description))
            .Select(description => description.Trim().ToLowerInvariant())
            .ToList();

        if (samples.Any(description => HighPriorityWeatherTerms.Any(term => description.Contains(term))))
        {
            return true;
        }

        var drizzleDayCount = samples.Count(description => SecondaryPriorityWeatherTerms.Any(term => description.Contains(term)));
        return drizzleDayCount >= WetWeekDrizzleDayThreshold;
    }

    private static PriorityMatchResult? FindByPriorityTerms(IEnumerable<DescriptionSample> samples, string[] priorityTerms)
    {
        foreach (var sample in samples)
        {
            foreach (var term in priorityTerms)
            {
                if (sample.Normalized.Contains(term, StringComparison.Ordinal))
                {
                    return new PriorityMatchResult(sample, term);
                }
            }
        }

        return null;
    }
}

internal sealed record QueryInfo(string Sql, IReadOnlyList<NpgsqlParameter> Parameters);

internal sealed record RawWeatherRow(
    DateTime Date,
    int WeatherCodeId,
    double Temperature,
    double Precipitation,
    double Humidity,
    string? WeatherMainDescription,
    int WeekNumber,
    int Year);

internal sealed record DengueWeeklyRecord(
    int Year,
    int WeekNumber,
    int CaseCount);

internal sealed record DominantWeatherCategoryResult(string Category, string? Description, bool HasPriorityMatch);

internal sealed record DescriptionSample(string Original, string Normalized);

internal sealed record PriorityMatchResult(DescriptionSample Sample, string MatchedTerm);

