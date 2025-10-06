namespace dengue.watch.api.common.models;

public sealed record WeeklyTrainingWeatherSnapshot(
    string PsgcCode,
    int DengueYear,
    int DengueWeekNumber,
    int DengueCaseCount,
    int LagYear,
    int LagWeekNumber,
    DateOnly LagWeekStartDate,
    WeeklyStatisticsResult Temperature,
    WeeklyStatisticsResult Humidity,
    WeeklyStatisticsResult Precipitation,
    int MostCommonWeatherCodeId,
    string? MostCommonWeatherDescription,
    int OccurrenceCount,
    string DominantWeatherCategory,
    bool IsWetWeek);

