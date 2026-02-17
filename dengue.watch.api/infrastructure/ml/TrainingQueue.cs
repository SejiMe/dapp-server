using System.Threading.Channels;

namespace dengue.watch.api.infrastructure.ml;

public class TrainingQueue : ITrainingQueue
{
    private readonly Channel<TrainingWorkItem> _channel;

    public TrainingQueue()
    {
        // Bounded channel to avoid unbounded queue growth
        var options = new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.Wait
        };
        _channel = Channel.CreateBounded<TrainingWorkItem>(options);
    }

    public string EnqueueTraining()
    {
        var id = Guid.NewGuid().ToString();
        var item = new TrainingWorkItem(id, DateTime.UtcNow);
        _channel.Writer.TryWrite(item);
        return id;
    }

    public ValueTask<TrainingWorkItem> DequeueAsync(CancellationToken cancellationToken)
    {
        return _channel.Reader.ReadAsync(cancellationToken);
    }
}
