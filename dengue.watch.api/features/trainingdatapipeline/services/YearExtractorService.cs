using System;

namespace dengue.watch.api.features.trainingdatapipeline.services;

public interface IYearExtractorService
{
    int ExtractYear(DateTime date);
    int ExtractYear(DateOnly date);
}

public class YearExtractorService : IYearExtractorService
{
    public int ExtractYear(DateTime date)
    {
        return date.Year;
    }

    public int ExtractYear(DateOnly date)
    {
        return date.Year;
    }
}

