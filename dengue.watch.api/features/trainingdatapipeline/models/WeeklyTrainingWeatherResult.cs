using dengue.watch.api.features.trainingdatapipeline.models;

public sealed record WeeklyTrainingWeatherResult(
    IReadOnlyCollection<WeeklyTrainingWeatherSnapshot> Snapshots,
    IReadOnlyCollection<string> MissingLagWeeks);
