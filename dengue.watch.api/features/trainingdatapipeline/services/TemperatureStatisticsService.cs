using System;
using System.Collections.Generic;
using System.Linq;
using dengue.watch.api.features.trainingdatapipeline.models;

namespace dengue.watch.api.features.trainingdatapipeline.services;

public interface ITemperatureStatisticsService
{
    WeeklyStatisticsResult CalculateWeeklyStatistics(IEnumerable<double> minimumTemperatures, IEnumerable<double> maximumTemperatures);
}

public class TemperatureStatisticsService : ITemperatureStatisticsService
{
    public WeeklyStatisticsResult CalculateWeeklyStatistics(IEnumerable<double> minimumTemperatures, IEnumerable<double> maximumTemperatures)
    {
        var minValues = WeeklyStatisticsCalculator.ValidateAndMaterialize(minimumTemperatures, nameof(minimumTemperatures));
        var maxValues = WeeklyStatisticsCalculator.ValidateAndMaterialize(maximumTemperatures, nameof(maximumTemperatures));

        var combinedMean = minValues.Zip(maxValues, (min, max) => (min + max) / 2).Average();
        var weeklyMax = maxValues.Max();

        return new WeeklyStatisticsResult(combinedMean, weeklyMax);
    }
}

