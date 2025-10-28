
namespace dengue.watch.api.common.models;

public sealed record WeeklyTrainingWeatherResult(
    List<WeeklyTrainingWeatherSnapshot> Snapshots,
    List<string> MissingLagWeeks);
