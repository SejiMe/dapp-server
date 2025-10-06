
namespace dengue.watch.api.common.models;

public sealed record WeeklyTrainingWeatherResult(
    IReadOnlyCollection<WeeklyTrainingWeatherSnapshot> Snapshots,
    IReadOnlyCollection<string> MissingLagWeeks);
