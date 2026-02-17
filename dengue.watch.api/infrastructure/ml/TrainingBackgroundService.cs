using Microsoft.AspNetCore.SignalR;
using dengue.watch.api.infrastructure.hubs;

namespace dengue.watch.api.infrastructure.ml;

public class TrainingBackgroundService : BackgroundService
{
    private readonly ITrainingQueue _queue;
    private readonly ILogger<TrainingBackgroundService> _logger;
    private readonly IServiceProvider _sp;
    private readonly IHubContext<NotificationHub> _hub;

    public TrainingBackgroundService(ITrainingQueue queue, IServiceProvider sp, ILogger<TrainingBackgroundService> logger, IHubContext<NotificationHub> hub)
    {
        _queue = queue;
        _sp = sp;
        _logger = logger;
        _hub = hub;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TrainingBackgroundService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            TrainingWorkItem item;
            try
            {
                item = await _queue.DequeueAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            _logger.LogInformation("Dequeued training operation {OpId}", item.OperationId);

            try
            {
                await _hub.Clients.All.SendAsync("TrainingStarted", new { OperationId = item.OperationId, Timestamp = DateTime.UtcNow });

                using var scope = _sp.CreateScope();
                var predictionService = scope.ServiceProvider.GetRequiredService<IPredictionService<AdvDengueForecastInput, DengueForecastOutput>>();
                var modelInfoStore = scope.ServiceProvider.GetRequiredService<ModelInfoStore>();

                var metrics = await predictionService.TrainModelAsync();

                // Persist model info (increments version)
                var info = modelInfoStore.SaveNewTrained("Advance Dengue Forecast Model", "Regression model for dengue case prediction with confidence intervals and outbreak probability with Geospatial Capability");

                await _hub.Clients.All.SendAsync("TrainingCompleted", new { OperationId = item.OperationId, ModelInfo = info, Metrics = metrics });
                _logger.LogInformation("Training operation {OpId} completed", item.OperationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Training operation {OpId} failed", item.OperationId);
                await _hub.Clients.All.SendAsync("TrainingFailed", new { OperationId = item.OperationId, Error = ex.Message });
            }
        }
    }
}
