namespace dengue.watch.api.features.trainingdatapipeline.models;

public sealed record TrainingDataWeatherRequest
{
    public required string PsgcCode { get; init; }
    public required IReadOnlyCollection<int> Years { get; init; }
    public int? WeekNumber { get; init; }
    public WeekRangeFilter? WeekRange { get; init; }
}

public sealed record WeekRangeFilter
{
    public required int From { get; init; }
    public required int To { get; init; }
}

