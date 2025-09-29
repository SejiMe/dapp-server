using System;
using System.Globalization;

namespace dengue.watch.api.features.trainingdatapipeline.services;

public interface IWeekExtractorService
{
    int ExtractWeek(DateTime date);
    int ExtractWeek(DateOnly date);
}

public class WeekExtractorService : IWeekExtractorService
{
    public int ExtractWeek(DateTime date)
    {
        return ISOWeek.GetWeekOfYear(date);
    }

    public int ExtractWeek(DateOnly date)
    {
        var dateTime = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        return ISOWeek.GetWeekOfYear(dateTime);
    }
}

