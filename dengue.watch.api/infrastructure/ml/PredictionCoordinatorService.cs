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
        var results = new List<PredictionResultRecord>();
        var errors = new List<PredictionErrorRecord>();

        try
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(psgcCode))
            {
                var error = "PsgcCode is required and cannot be empty";
                _logger.LogError("RunForPsgcAsync failed: {Error}", error);
                return PredictionCoordinatorResult.Failure(psgcCode ?? "unknown", aggregatedYear, aggregatedWeek, error, nameof(ArgumentException));
            }

            if (aggregatedWeek < 1 || aggregatedWeek > 53)
            {
                var error = "AggregatedWeek must be between 1 and 53";
                _logger.LogError("RunForPsgcAsync failed for {PsgcCode}: {Error}", psgcCode, error);
                return PredictionCoordinatorResult.Failure(psgcCode, aggregatedYear, aggregatedWeek, error, nameof(ValidationException));
            }

            // 1. Fetch aggregated weekly historical weather snapshot for the lag week
            AggregatedWeeklyHistoricalWeatherSnapshot? snapshot;
            try
            {
                snapshot = await _repo.GetWeeklyHistoricalWeatherSnapshotAsync(psgcCode, aggregatedYear, aggregatedWeek, cancellation);
                
                if (snapshot is null)
                {
                    _logger.LogWarning("No weather data available for {PsgcCode} at Year={Year}, Week={Week}. Skipping prediction.", psgcCode, aggregatedYear, aggregatedWeek);
                    return PredictionCoordinatorResult.Failure(psgcCode, aggregatedYear, aggregatedWeek, 
                        $"No weather data available for the specified period (Year={aggregatedYear}, Week={aggregatedWeek})", 
                        "MissingWeatherData");
                }
            }
            catch (ValidationException ve)
            {
                _logger.LogError(ve, "Failed to fetch weather snapshot for {PsgcCode} at Year={Year}, Week={Week}", psgcCode, aggregatedYear, aggregatedWeek);
                return PredictionCoordinatorResult.Failure(psgcCode, aggregatedYear, aggregatedWeek, $"Weather snapshot not available: {ve.Message}", nameof(ValidationException));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error fetching weather snapshot for {PsgcCode} at Year={Year}, Week={Week}", psgcCode, aggregatedYear, aggregatedWeek);
                return PredictionCoordinatorResult.Failure(psgcCode, aggregatedYear, aggregatedWeek, $"Failed to fetch weather snapshot: {ex.Message}", ex.GetType().Name);
            }

            // 2. Build AdvDengueForecastInput from snapshot
            var advInput = MapSnapshotToAdvInput(psgcCode, snapshot);

            // 3. Initial 1-year ahead prediction (placeholder long-term)
            DengueForecastOutput initialPred;
            try
            {
                initialPred = await _predictionService.PredictAsync(advInput);
            }
            catch (InvalidOperationException ioe)
            {
                _logger.LogError(ioe, "ML model not loaded for initial prediction for {PsgcCode}", psgcCode);
                return PredictionCoordinatorResult.Failure(psgcCode, aggregatedYear, aggregatedWeek, $"ML model error: {ioe.Message}", nameof(InvalidOperationException));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to run initial prediction for {PsgcCode}", psgcCode);
                return PredictionCoordinatorResult.Failure(psgcCode, aggregatedYear, aggregatedWeek, $"Initial prediction failed: {ex.Message}", ex.GetType().Name);
            }

            // Compute predicted iso year/week (year +1)
            var predictedYear = aggregatedYear + 1;
            var predictedWeek = aggregatedWeek;

            try
            {
                var createdOrUpdated = await UpsertPredictionRecord(psgcCode, advInput, predictedYear, predictedWeek, initialPred, cancellation);
                results.Add(createdOrUpdated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upsert initial prediction for {PsgcCode} at Year={Year}, Week={Week}", psgcCode, predictedYear, predictedWeek);
                errors.Add(new PredictionErrorRecord(psgcCode, predictedYear, predictedWeek, $"Failed to save initial prediction: {ex.Message}", ex.GetType().Name));
            }

            // 4. Short-term follow-up: predict for 2 weeks ahead logic (example: aggregated week 3 -> predict for week 5)
            var followupDate = ISOWeek.ToDateTime(aggregatedYear, aggregatedWeek, DayOfWeek.Monday).AddDays(14);
            var followupYear = ISOWeek.GetYear(followupDate);
            var followupWeek = ISOWeek.GetWeekOfYear(followupDate);

            var followupInput = MapSnapshotToAdvInput(psgcCode, snapshot);

            DengueForecastOutput followupPred;
            try
            {
                followupPred = await _predictionService.PredictAsync(followupInput);
            }
            catch (InvalidOperationException ioe)
            {
                _logger.LogError(ioe, "ML model not loaded for follow-up prediction for {PsgcCode}", psgcCode);
                errors.Add(new PredictionErrorRecord(psgcCode, followupYear, followupWeek, $"ML model error: {ioe.Message}", nameof(InvalidOperationException)));
                return PredictionCoordinatorResult.PartialSuccess(results, errors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to run follow-up prediction for {PsgcCode}", psgcCode);
                errors.Add(new PredictionErrorRecord(psgcCode, followupYear, followupWeek, $"Follow-up prediction failed: {ex.Message}", ex.GetType().Name));
                return PredictionCoordinatorResult.PartialSuccess(results, errors);
            }

            try
            {
                var followupRecord = await UpsertPredictionRecord(psgcCode, followupInput, followupYear, followupWeek, followupPred, cancellation);
                results.Add(followupRecord);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upsert follow-up prediction for {PsgcCode} at Year={Year}, Week={Week}", psgcCode, followupYear, followupWeek);
                errors.Add(new PredictionErrorRecord(psgcCode, followupYear, followupWeek, $"Failed to save follow-up prediction: {ex.Message}", ex.GetType().Name));
            }

            return PredictionCoordinatorResult.PartialSuccess(results, errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in RunForPsgcAsync for {PsgcCode} at Year={Year}, Week={Week}", psgcCode, aggregatedYear, aggregatedWeek);
            return PredictionCoordinatorResult.Failure(psgcCode, aggregatedYear, aggregatedWeek, $"Unexpected error: {ex.Message}", ex.GetType().Name);
        }
    }

    public async Task<BulkPredictionResult> RunForAllAsync(CancellationToken cancellation = default)
    {
        var errors = new List<PredictionErrorRecord>();
        int processed = 0;
        int skipped = 0;

        List<string> barangays;
        try
        {
            barangays = await _db.AdministrativeAreas
                .Where(a => a.GeographicLevel == "Bgy" && a.Latitude.HasValue && a.Longitude.HasValue)
                .Select(a => a.PsgcCode)
                .ToListAsync(cancellation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch barangays from database");
            errors.Add(new PredictionErrorRecord("N/A", null, null, $"Failed to fetch barangays: {ex.Message}", ex.GetType().Name));
            return new BulkPredictionResult(0, 0, 1, 0, errors);
        }

        _logger.LogInformation("Starting RunForAllAsync with {Count} barangays", barangays.Count);

        foreach (var psgc in barangays)
        {
            try
            {
                var lastWeekly = await _db.WeeklyDengueCases
                    .Where(w => w.PsgcCode == psgc)
                    .OrderByDescending(w => w.Year)
                    .ThenByDescending(w => w.WeekNumber)
                    .FirstOrDefaultAsync(cancellation);

                if (lastWeekly == null)
                {
                    _logger.LogDebug("No weekly dengue records for {Psgc}", psgc);
                    skipped++;
                    continue;
                }

                var dengueYear = lastWeekly.Year;
                var dengueWeek = lastWeekly.WeekNumber;

                var date = ISOWeek.ToDateTime(dengueYear, dengueWeek, DayOfWeek.Monday);
                var lagDate = date.AddDays(-14);
                var lagYear = ISOWeek.GetYear(lagDate);
                var lagWeek = ISOWeek.GetWeekOfYear(lagDate);

                // Check if aggregated snapshot exists
                try
                {
                    await _repo.GetWeeklyHistoricalWeatherSnapshotAsync(psgc, lagYear, lagWeek, cancellation);
                }
                catch (ValidationException)
                {
                    _logger.LogDebug("Snapshot not available for {Psgc} at Year={Year}, Week={Week}", psgc, lagYear, lagWeek);
                    skipped++;
                    continue;
                }

                // Run predictions
                var result = await RunForPsgcAsync(psgc, lagYear, lagWeek, cancellation);
                
                if (result.IsSuccess)
                {
                    processed++;
                }
                else
                {
                    errors.AddRange(result.Errors);
                    _logger.LogWarning("Prediction failed for {Psgc} at Year={Year}, Week={Week}: {ErrorCount} errors", psgc, lagYear, lagWeek, result.Errors.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process predictions for {Psgc}", psgc);
                errors.Add(new PredictionErrorRecord(psgc, null, null, $"Unexpected error: {ex.Message}", ex.GetType().Name));
            }
        }

        _logger.LogInformation("RunForAllAsync completed. {Summary}", new BulkPredictionResult(processed, skipped, errors.Count, barangays.Count, errors).Summary);

        return new BulkPredictionResult(processed, skipped, errors.Count, barangays.Count, errors);
    }

    public async Task<BulkPredictionResult> RunForAllByWeekAsync(int aggregatedYear, int aggregatedWeek, CancellationToken cancellation = default)
    {
        var errors = new List<PredictionErrorRecord>();
        int processed = 0;
        int skipped = 0;

        if (aggregatedWeek < 1 || aggregatedWeek > 53)
        {
            var error = "AggregatedWeek must be between 1 and 53";
            _logger.LogError("RunForAllByWeekAsync validation failed: {Error}", error);
            errors.Add(new PredictionErrorRecord("N/A", aggregatedYear, aggregatedWeek, error, nameof(ValidationException)));
            return new BulkPredictionResult(0, 0, 1, 0, errors);
        }

        List<string> barangays;
        try
        {
            barangays = await _db.AdministrativeAreas
                .Where(a => a.GeographicLevel == "Bgy" && a.Latitude.HasValue && a.Longitude.HasValue)
                .Select(a => a.PsgcCode)
                .ToListAsync(cancellation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch barangays from database");
            errors.Add(new PredictionErrorRecord("N/A", aggregatedYear, aggregatedWeek, $"Failed to fetch barangays: {ex.Message}", ex.GetType().Name));
            return new BulkPredictionResult(0, 0, 1, 0, errors);
        }

        _logger.LogInformation("Starting RunForAllByWeekAsync for Year={Year}, Week={Week} with {Count} barangays", aggregatedYear, aggregatedWeek, barangays.Count);

        foreach (var psgc in barangays)
        {
            try
            {
                // Check if aggregated snapshot exists for this psgc and week
                try
                {
                    await _repo.GetWeeklyHistoricalWeatherSnapshotAsync(psgc, aggregatedYear, aggregatedWeek, cancellation);
                }
                catch (ValidationException)
                {
                    _logger.LogDebug("No snapshot available for {Psgc} at Year={Year}, Week={Week}", psgc, aggregatedYear, aggregatedWeek);
                    skipped++;
                    continue;
                }

                // Run predictions using the specified aggregated week
                var result = await RunForPsgcAsync(psgc, aggregatedYear, aggregatedWeek, cancellation);
                
                if (result.IsSuccess)
                {
                    processed++;
                }
                else
                {
                    errors.AddRange(result.Errors);
                    _logger.LogWarning("Prediction failed for {Psgc} at Year={Year}, Week={Week}: {ErrorCount} errors", psgc, aggregatedYear, aggregatedWeek, result.Errors.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process predictions for {Psgc} at Year={Year}, Week={Week}", psgc, aggregatedYear, aggregatedWeek);
                errors.Add(new PredictionErrorRecord(psgc, aggregatedYear, aggregatedWeek, $"Unexpected error: {ex.Message}", ex.GetType().Name));
            }
        }

        _logger.LogInformation("RunForAllByWeekAsync completed for Year={Year}, Week={Week}. {Summary}", aggregatedYear, aggregatedWeek, new BulkPredictionResult(processed, skipped, errors.Count, barangays.Count, errors).Summary);

        return new BulkPredictionResult(processed, skipped, errors.Count, barangays.Count, errors);
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
        var existing = await _db.PredictedWeeklyDengues.FirstOrDefaultAsync(p => p.PsgcCode == psgc && p.PredictedIsoYear == predictedYear && p.PredictedIsoWeek == predictedWeek, cancellation);

        if (existing != null)
        {
            existing.LaggedIsoWeek = input.LagWeekNumber;
            existing.LaggedIsoYear = input.LagYear;
            existing.PredictedValue = Convert.ToInt32(Math.Round(Convert.ToDecimal(prediction.Score), 2));
            existing.LowerBound = prediction.LowerBound;
            existing.UpperBound = prediction.UpperBound;
            existing.ConfidencePercentage = prediction.ConfidencePercentage;
            existing.ProbabilityOfOutbreak = prediction.ProbabilityOfOutbreak;
            existing.RiskLevel = prediction.GetRiskLevel();

            await _db.SaveChangesAsync(cancellation);

            _logger.LogDebug("Updated prediction for {Psgc} at Year={Year}, Week={Week}", psgc, predictedYear, predictedWeek);

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

        _logger.LogDebug("Created prediction for {Psgc} at Year={Year}, Week={Week}", psgc, predictedYear, predictedWeek);

        return new PredictionResultRecord(psgc, predictedYear, predictedWeek, true, dCase.PredictionId, dCase.PredictedValue);
    }
}
