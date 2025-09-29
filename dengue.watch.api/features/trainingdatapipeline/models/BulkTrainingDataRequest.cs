namespace dengue.watch.api.features.trainingdatapipeline.models;

public sealed record BulkTrainingDataRequest
{
    public required IReadOnlyCollection<int> Years { get; init; }
}

