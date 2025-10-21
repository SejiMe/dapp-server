namespace dengue.watch.api.common.models;

public sealed record AggregatedWeeklyHistoricalWeatherSnapshot
(
    string psgccode,
    int LagIsoYear,
    int LagIsoWeek,
    WeeklyStatisticsResult Temperature,
    WeeklyStatisticsResult Humidity,
    WeeklyStatisticsResult Precipitation,
    int ModeCommonWeatherCodeId,
    int OccurentCount,
    string DominantWeatherCategory,
    bool IsWetWeek
);