

namespace dengue.watch.api.common.services;

public interface IWeeklyDataStatisticsService
{
    WeeklyStatisticsResult CalculateTemperatureStatistics(IEnumerable<double> minimumTemperatures, IEnumerable<double> maximumTemperatures);
    WeeklyStatisticsResult CalculateHumidityStatistics(IEnumerable<double> minimumHumidity, IEnumerable<double> maximumHumidity);
    WeeklyStatisticsResult CalculatePrecipitationStatistics(IEnumerable<double> minimumPrecipitation, IEnumerable<double> maximumPrecipitation);
    int CountWeatherCodeOccurrences(IEnumerable<int> weatherCodeIds, int targetWeatherCodeId);
    int CountWeatherCodeOccurrences(IEnumerable<string> weatherCodeDescriptions, string targetDescription);
}

public class WeeklyDataStatisticsService : IWeeklyDataStatisticsService
{
    private readonly ITemperatureStatisticsService _temperatureStatisticsService;
    private readonly IHumidityStatisticsService _humidityStatisticsService;
    private readonly IPrecipitationStatisticsService _precipitationStatisticsService;
    private readonly IWeatherCodeStatisticsService _weatherCodeStatisticsService;

    public WeeklyDataStatisticsService(
        ITemperatureStatisticsService temperatureStatisticsService,
        IHumidityStatisticsService humidityStatisticsService,
        IPrecipitationStatisticsService precipitationStatisticsService,
        IWeatherCodeStatisticsService weatherCodeStatisticsService)
    {
        _temperatureStatisticsService = temperatureStatisticsService;
        _humidityStatisticsService = humidityStatisticsService;
        _precipitationStatisticsService = precipitationStatisticsService;
        _weatherCodeStatisticsService = weatherCodeStatisticsService;
    }

    public WeeklyStatisticsResult CalculateTemperatureStatistics(IEnumerable<double> minimumTemperatures, IEnumerable<double> maximumTemperatures)
    {
        return _temperatureStatisticsService.CalculateWeeklyStatistics(minimumTemperatures, maximumTemperatures);
    }

    public WeeklyStatisticsResult CalculateHumidityStatistics(IEnumerable<double> minimumHumidity, IEnumerable<double> maximumHumidity)
    {
        return _humidityStatisticsService.CalculateWeeklyStatistics(minimumHumidity, maximumHumidity);
    }

    public WeeklyStatisticsResult CalculatePrecipitationStatistics(IEnumerable<double> minimumPrecipitation, IEnumerable<double> maximumPrecipitation)
    {
        return _precipitationStatisticsService.CalculateWeeklyStatistics(minimumPrecipitation, maximumPrecipitation);
    }

    public int CountWeatherCodeOccurrences(IEnumerable<int> weatherCodeIds, int targetWeatherCodeId)
    {
        return _weatherCodeStatisticsService.CountOccurrences(weatherCodeIds, targetWeatherCodeId);
    }

    public int CountWeatherCodeOccurrences(IEnumerable<string> weatherCodeDescriptions, string targetDescription)
    {
        return _weatherCodeStatisticsService.CountOccurrences(weatherCodeDescriptions, targetDescription);
    }
}

