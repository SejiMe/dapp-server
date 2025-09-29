using System;
using System.Collections.Generic;
using System.Linq;
using dengue.watch.api.features.trainingdatapipeline.models;

namespace dengue.watch.api.features.trainingdatapipeline.services;

public interface IHumidityStatisticsService
{
    WeeklyStatisticsResult CalculateWeeklyStatistics(IEnumerable<double> minimumHumidity, IEnumerable<double> maximumHumidity);
}

public class HumidityStatisticsService : IHumidityStatisticsService
{
    public WeeklyStatisticsResult CalculateWeeklyStatistics(IEnumerable<double> minimumHumidity, IEnumerable<double> maximumHumidity)
    {
        var minValues = WeeklyStatisticsCalculator.ValidateAndMaterialize(minimumHumidity, nameof(minimumHumidity));
        var maxValues = WeeklyStatisticsCalculator.ValidateAndMaterialize(maximumHumidity, nameof(maximumHumidity));

        var combinedMean = minValues.Zip(maxValues, (min, max) => (min + max) / 2).Average();
        var weeklyMax = maxValues.Max();

        return new WeeklyStatisticsResult(combinedMean, weeklyMax);
    }
}

