using System.Threading.Channels;

namespace dengue.watch.api.infrastructure.ml;

public interface ITrainingQueue
{
    string EnqueueTraining();
    ValueTask<TrainingWorkItem> DequeueAsync(CancellationToken cancellationToken);
}

public record TrainingWorkItem(string OperationId, DateTime EnqueuedAt);
