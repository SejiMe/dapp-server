using dengue.watch.api.infrastructure.ml;
using Quartz;

namespace dengue.watch.api.features.denguecases.jobs;

public class AdvancePredictionCoordinatorJob : IJob
{
    private readonly IPredictionCoordinator _coordinator;
    private readonly ILogger<AdvancePredictionCoordinatorJob> _logger;

    public AdvancePredictionCoordinatorJob(IPredictionCoordinator coordinator, ILogger<AdvancePredictionCoordinatorJob> logger)
    {
        _coordinator = coordinator;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("AdvancePredictionCoordinatorJob started at {Time}", DateTimeOffset.UtcNow);

        try
        {
            var result = await _coordinator.RunForAllAsync(context.CancellationToken);
            
            _logger.LogInformation("AdvancePredictionCoordinatorJob completed. {Summary}", result.Summary);
            
            if (result.HasErrors)
            {
                _logger.LogWarning("AdvancePredictionCoordinatorJob encountered {ErrorCount} errors", result.ErrorCount);
                foreach (var error in result.Errors.Take(10)) // Log first 10 errors to avoid log flooding
                {
                    _logger.LogWarning("Prediction error for {PsgcCode} (Year={Year}, Week={Week}): {ErrorMessage} [{ErrorType}]",
                        error.PsgcCode, error.AggregatedYear, error.AggregatedWeek, error.ErrorMessage, error.ErrorType);
                }
                
                if (result.Errors.Count > 10)
                {
                    _logger.LogWarning("... and {RemainingCount} more errors", result.Errors.Count - 10);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AdvancePredictionCoordinatorJob failed with unexpected error");
        }

        _logger.LogInformation("AdvancePredictionCoordinatorJob finished at {Time}", DateTimeOffset.UtcNow);
    }
}
