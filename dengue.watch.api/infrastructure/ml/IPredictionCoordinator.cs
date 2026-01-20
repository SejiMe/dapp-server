namespace dengue.watch.api.infrastructure.ml;

public interface IPredictionCoordinator
{
    /// <summary>
    /// Run initial (1-year ahead) and short-term follow-up (2-weeks ahead) predictions for a single PSGC
    /// </summary>
    Task<PredictionCoordinatorResult> RunForPsgcAsync(string psgcCode, int aggregatedYear, int aggregatedWeek, CancellationToken cancellation = default);

    /// <summary>
    /// Run predictions for all barangays using current lagged week derived from DateExtraction
    /// </summary>
    Task<BulkPredictionResult> RunForAllAsync(CancellationToken cancellation = default);

    /// <summary>
    /// Run predictions for all barangays using a specific aggregated ISO week and year
    /// </summary>
    /// <param name="aggregatedYear">The aggregated ISO year to use for all predictions</param>
    /// <param name="aggregatedWeek">The aggregated ISO week to use for all predictions</param>
    /// <param name="cancellation">Cancellation token</param>
    /// <returns>Bulk prediction result with processed count and errors</returns>
    Task<BulkPredictionResult> RunForAllByWeekAsync(int aggregatedYear, int aggregatedWeek, CancellationToken cancellation = default);
}

/// <summary>
/// Represents a successful prediction result for a single PSGC and week
/// </summary>
public record PredictionResultRecord(
    string PsgcCode, 
    int PredictedYear, 
    int PredictedWeek, 
    bool Created, 
    Guid PredictionId, 
    int PredictedValue);

/// <summary>
/// Represents an error that occurred during prediction processing
/// </summary>
public record PredictionErrorRecord(
    string PsgcCode,
    int? AggregatedYear,
    int? AggregatedWeek,
    string ErrorMessage,
    string? ErrorType);

/// <summary>
/// Result of a single PSGC prediction coordination (initial + follow-up)
/// </summary>
public record PredictionCoordinatorResult(
    List<PredictionResultRecord> Results,
    List<PredictionErrorRecord> Errors,
    bool IsSuccess)
{
    public static PredictionCoordinatorResult Success(List<PredictionResultRecord> results) 
        => new(results, [], true);
    
    public static PredictionCoordinatorResult Failure(string psgcCode, int? year, int? week, string errorMessage, string? errorType = null)
        => new([], [new PredictionErrorRecord(psgcCode, year, week, errorMessage, errorType)], false);
    
    public static PredictionCoordinatorResult PartialSuccess(List<PredictionResultRecord> results, List<PredictionErrorRecord> errors)
        => new(results, errors, errors.Count == 0);
}

/// <summary>
/// Result of bulk prediction processing for all barangays
/// </summary>
public record BulkPredictionResult(
    int ProcessedCount,
    int SkippedCount,
    int ErrorCount,
    int TotalBarangays,
    List<PredictionErrorRecord> Errors)
{
    public bool HasErrors => ErrorCount > 0;
    public string Summary => $"Processed: {ProcessedCount}, Skipped: {SkippedCount}, Errors: {ErrorCount}, Total: {TotalBarangays}";
}