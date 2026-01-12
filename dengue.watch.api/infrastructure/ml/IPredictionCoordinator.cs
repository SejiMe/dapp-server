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
    Task<int> RunForAllAsync(CancellationToken cancellation = default);
}

public record PredictionResultRecord(string PsgcCode, int PredictedYear, int PredictedWeek, bool Created, Guid PredictionId, int PredictedValue);

public record PredictionCoordinatorResult(List<PredictionResultRecord> Results);