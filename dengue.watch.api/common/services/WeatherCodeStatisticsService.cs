namespace dengue.watch.api.common.services;

public interface IWeatherCodeStatisticsService
{
    int CountOccurrences(IEnumerable<int> weatherCodeIds, int targetWeatherCodeId);
    int CountOccurrences(IEnumerable<string> weatherCodeDescriptions, string targetDescription);
}

public class WeatherCodeStatisticsService : IWeatherCodeStatisticsService
{
    public int CountOccurrences(IEnumerable<int> weatherCodeIds, int targetWeatherCodeId)
    {
        if (weatherCodeIds == null)
        {
            throw new ArgumentNullException(nameof(weatherCodeIds));
        }

        var values = ValidateAndMaterialize(weatherCodeIds, nameof(weatherCodeIds));

        return values.Count(codeId => codeId == targetWeatherCodeId);
    }

    public int CountOccurrences(IEnumerable<string> weatherCodeDescriptions, string targetDescription)
    {
        if (weatherCodeDescriptions == null)
        {
            throw new ArgumentNullException(nameof(weatherCodeDescriptions));
        }

        if (targetDescription == null)
        {
            throw new ArgumentNullException(nameof(targetDescription));
        }

        var values = ValidateAndMaterialize(weatherCodeDescriptions, nameof(weatherCodeDescriptions));

        return values.Count(description => string.Equals(description, targetDescription, StringComparison.OrdinalIgnoreCase));
    }

    private static IReadOnlyList<T> ValidateAndMaterialize<T>(IEnumerable<T> values, string parameterName)
    {
        var data = values as IList<T> ?? values.ToList();

        if (data.Count != 7)
        {
            throw new ArgumentException("Weekly data must contain exactly 7 elements.", parameterName);
        }

        return data as IReadOnlyList<T> ?? data.ToArray();
    }
}

