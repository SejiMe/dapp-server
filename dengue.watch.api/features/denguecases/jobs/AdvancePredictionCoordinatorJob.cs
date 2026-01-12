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
            var processed = await _coordinator.RunForAllAsync(context.CancellationToken);
            _logger.LogInformation("AdvancePredictionCoordinatorJob processed {Count} barangays", processed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AdvancePredictionCoordinatorJob failed");
        }

        _logger.LogInformation("AdvancePredictionCoordinatorJob finished at {Time}", DateTimeOffset.UtcNow);
    }
}
