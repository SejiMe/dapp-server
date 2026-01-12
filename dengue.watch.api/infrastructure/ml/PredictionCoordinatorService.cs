using System.Globalization;
using dengue.watch.api.common.models;
using dengue.watch.api.common.repositories;
using dengue.watch.api.infrastructure.database;
using Microsoft.EntityFrameworkCore;

namespace dengue.watch.api.infrastructure.ml;

/// <summary>
/// Coordinates prediction workflow using AdvanceDengueForecastService and repository snapshots
/// </summary>
public class PredictionCoordinatorService : IPredictionCoordinator
{
    private readonly IAggregatedWeeklyHistoricalWeatherRepository _repo;
    private readonly IPredictionService<AdvDengueForecastInput, DengueForecastOutput> _predictionService;
    private readonly ApplicationDbContext _db;
    private readonly ILogger<PredictionCoordinatorService> _logger;

    public PredictionCoordinatorService(
        IAggregatedWeeklyHistoricalWeatherRepository repo,
        IPredictionService<AdvDengueForecastInput, DengueForecastOutput> predictionService,
        ApplicationDbContext db,
        ILogger<PredictionCoordinatorService> logger)
    {
        _repo = repo;
        _predictionService = predictionService;
        _db = db;
        _logger = logger;
    }

    public async Task<PredictionCoordinatorResult> RunForPsgcAsync(string psgcCode, int aggregatedYear, int aggregatedWeek, CancellationToken cancellation = default)
    {
        // 1. Fetch aggregated weekly historical weather snapshot for the lag week
        var snapshot = await _repo.GetWeeklyHistoricalWeatherSnapshotAsync(psgcCode, aggregatedYear, aggregatedWeek, cancellation);

        // 2. Build AdvDengueForecastInput from snapshot
        var advInput = MapSnapshotToAdvInput(psgcCode, snapshot);

        // 3. Initial 1-year ahead prediction (placeholder long-term)
        var initialPred = await _predictionService.PredictAsync(advInput);

        // Compute predicted iso year/week (year +1)
        var predictedYear = aggregatedYear + 1;
        var predictedWeek = aggregatedWeek;

        var createdOrUpdated = await UpsertPredictionRecord(psgcCode, advInput, predictedYear, predictedWeek, initialPred, cancellation);

        var results = new List<PredictionResultRecord>
        {
            createdOrUpdated
        };

        // 4. Short-term follow-up: predict for 2 weeks ahead logic (example: aggregated week 3 -> predict for week 5)
        // Determine target week as aggregatedWeek + 2 (with ISO week rollover)
        var followupDate = System.Globalization.ISOWeek.ToDateTime(aggregatedYear, aggregatedWeek, DayOfWeek.Monday).AddDays(14);
        var followupYear = System.Globalization.ISOWeek.GetYear(followupDate);
        var followupWeek = System.Globalization.ISOWeek.GetWeekOfYear(followupDate);

        // Reuse same snapshot mapping (domain-specific logic could modify snapshot values)
        var followupInput = MapSnapshotToAdvInput(psgcCode, snapshot);

        var followupPred = await _predictionService.PredictAsync(followupInput);

        var followupRecord = await UpsertPredictionRecord(psgcCode, followupInput, followupYear, followupWeek, followupPred, cancellation);

        results.Add(followupRecord);

        return new PredictionCoordinatorResult(results);
    }

    public async Task<int> RunForAllAsync(CancellationToken cancellation = default)
    {
        // Determine latest aggregated weeks available in DB and run for each barangay where snapshot exists
        // For simplicity use PredictedWeeklyDengues table aggregation to find unique PSGC codes and the latest lag week available
        var barangays = await _db.AdministrativeAreas
            .Where(a => a.GeographicLevel == "Bgy" && a.Latitude.HasValue && a.Longitude.HasValue)
            .Select(a => a.PsgcCode)
            .ToListAsync(cancellation);

        int processed = 0;

        foreach (var psgc in barangays)
        {
            try
            {
                // For each psgc, attempt to get latest weekly dengue record to compute aggregated week
                // Fallback: use last week's data from WeeklyDengueCases if available
                var lastWeekly = await _db.WeeklyDengueCases
                    .Where(w => w.PsgcCode == psgc)
                    .OrderByDescending(w => w.Year)
                    .ThenByDescending(w => w.WeekNumber)
                    .FirstOrDefaultAsync(cancellation);

                if (lastWeekly == null)
                {
                    _logger.LogDebug("No weekly dengue records for {Psgc}", psgc);
                    continue;
                }

                var dengueYear = lastWeekly.Year;
                var dengueWeek = lastWeekly.WeekNumber;

                // Calculate lag week used by repository (already uses -14 days internally), use CalculateLagWeek logic by calling repository private method indirectly by reusing expected inputs
                // Here we approximate aggregated week as CalculateLagWeek(dengueYear, dengueWeek)
                var date = System.Globalization.ISOWeek.ToDateTime(dengueYear, dengueWeek, DayOfWeek.Monday);
                var lagDate = date.AddDays(-14);
                var lagYear = System.Globalization.ISOWeek.GetYear(lagDate);
                var lagWeek = System.Globalization.ISOWeek.GetWeekOfYear(lagDate);

                // Check if aggregated snapshot exists (call repository)
                try
                {
                    var snapshot = await _repo.GetWeeklyHistoricalWeatherSnapshotAsync(psgc, lagYear, lagWeek, cancellation);
                }
                catch (ValidationException)
                {
                    // Snapshot not available for this psgc and lag week
                    continue;
                }

                // Run predictions
                await RunForPsgcAsync(psgc, lagYear, lagWeek, cancellation);
                processed++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process predictions for {Psgc}", psgc);
            }
        }

        return processed;
    }

    private static AdvDengueForecastInput MapSnapshotToAdvInput(string psgc, AggregatedWeeklyHistoricalWeatherSnapshot snapshot)
    {
        return new AdvDengueForecastInput
        {
            PsgcCode = psgc,
            TemperatureMean = (float)snapshot.Temperature.Mean,
            TemperatureMax = (float)snapshot.Temperature.Max,
            HumidityMean = (float)snapshot.Humidity.Mean,
            HumidityMax = (float)snapshot.Humidity.Max,
            PrecipitationMean = (float)snapshot.Precipitation.Mean,
            PrecipitationMax = (float)snapshot.Precipitation.Max,
            IsWetWeek = snapshot.IsWetWeek ? "TRUE" : "FALSE",
            DominantWeatherCategory = snapshot.DominantWeatherCategory
        };
    }

    private async Task<PredictionResultRecord> UpsertPredictionRecord(string psgc, AdvDengueForecastInput input, int predictedYear, int predictedWeek, DengueForecastOutput prediction, CancellationToken cancellation)
    {
        // Check if exists
        var existing = await _db.PredictedWeeklyDengues.FirstOrDefaultAsync(p => p.PsgcCode == psgc && p.PredictedIsoYear == predictedYear && p.PredictedIsoWeek == predictedWeek, cancellation);

        if (existing != null)
        {
            existing.LaggedIsoWeek = input.LagWeekNumber; // input may not have LagWeekNumber set; optional
            existing.LaggedIsoYear = input.LagYear;
            existing.PredictedValue = Convert.ToInt32(Math.Round(Convert.ToDecimal(prediction.Score), 2));
            existing.LowerBound = prediction.LowerBound;
            existing.UpperBound = prediction.UpperBound;
            existing.ConfidencePercentage = prediction.ConfidencePercentage;
            existing.ProbabilityOfOutbreak = prediction.ProbabilityOfOutbreak;
            existing.RiskLevel = prediction.GetRiskLevel();

            await _db.SaveChangesAsync(cancellation);

            return new PredictionResultRecord(psgc, predictedYear, predictedWeek, false, existing.PredictionId, existing.PredictedValue);
        }

        var dCase = new PredictedWeeklyDengueCase
        {
            PsgcCode = psgc,
            LaggedIsoWeek = input.LagWeekNumber,
            LaggedIsoYear = input.LagYear,
            PredictedIsoWeek = predictedWeek,
            PredictedIsoYear = predictedYear,
            PredictedValue = Convert.ToInt32(Math.Round(Convert.ToDecimal(prediction.Score), 2)),
            LowerBound = prediction.LowerBound,
            UpperBound = prediction.UpperBound,
            ConfidencePercentage = prediction.ConfidencePercentage,
            ProbabilityOfOutbreak = prediction.ProbabilityOfOutbreak,
            RiskLevel = prediction.GetRiskLevel(),
            MonthName = IsoWeekHelper.GetMonthNameFromIsoWeek(predictedYear, predictedWeek)
        };

        await _db.PredictedWeeklyDengues.AddAsync(dCase, cancellation);
        await _db.SaveChangesAsync(cancellation);

        return new PredictionResultRecord(psgc, predictedYear, predictedWeek, true, dCase.PredictionId, dCase.PredictedValue);
    }
}
