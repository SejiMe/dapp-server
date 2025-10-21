
namespace dengue.watch.api.common.services;

internal static class WeeklyStatisticsCalculator
{
    public static WeeklyStatisticsResult Calculate(IEnumerable<double> values, string parameterName)
    {
        var data = ValidateAndMaterialize(values, parameterName);

        var mean = data.Average();
        var max = data.Max();

        return new WeeklyStatisticsResult(mean, max);
    }

    public static IReadOnlyList<double> ValidateAndMaterialize(IEnumerable<double> values, string parameterName)
    {
        if (values == null)
        {
            throw new ArgumentNullException(parameterName);
        }

        var data = values as IList<double> ?? values.ToList();

        ValidateWeeklyLength(data.Count, parameterName);

        if (data.Any(double.IsNaN))
        {
            throw new ArgumentException("Weekly data cannot contain NaN values.", parameterName);
        }

        if (data.Any(double.IsInfinity))
        {
            throw new ArgumentException("Weekly data cannot contain infinite values.", parameterName);
        }

        return data as IReadOnlyList<double> ?? data.ToArray();
    }

    private static void ValidateWeeklyLength(int count, string parameterName)
    {
        if (count > 7)
        {
            throw new ArgumentException("Weekly data must contain 7 OR below elements.", parameterName);
        }
    }
}

