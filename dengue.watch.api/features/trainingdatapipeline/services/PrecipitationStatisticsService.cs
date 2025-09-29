using System;
using System.Collections.Generic;
using System.Linq;
using dengue.watch.api.features.trainingdatapipeline.models;

namespace dengue.watch.api.features.trainingdatapipeline.services;

public interface IPrecipitationStatisticsService
{
    WeeklyStatisticsResult CalculateWeeklyStatistics(IEnumerable<double> minimumPrecipitation, IEnumerable<double> maximumPrecipitation);
}

public class PrecipitationStatisticsService : IPrecipitationStatisticsService
{
    public WeeklyStatisticsResult CalculateWeeklyStatistics(IEnumerable<double> minimumPrecipitation, IEnumerable<double> maximumPrecipitation)
    {
        var minValues = WeeklyStatisticsCalculator.ValidateAndMaterialize(minimumPrecipitation, nameof(minimumPrecipitation));
        var maxValues = WeeklyStatisticsCalculator.ValidateAndMaterialize(maximumPrecipitation, nameof(maximumPrecipitation));

        var combinedMean = minValues.Zip(maxValues, (min, max) => (min + max) / 2).Average();
        var weeklyMax = maxValues.Max();

        return new WeeklyStatisticsResult(combinedMean, weeklyMax);
    }
}

