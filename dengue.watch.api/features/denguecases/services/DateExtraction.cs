using System.Globalization;

namespace dengue.watch.api.features.denguecases.services;

public class DateExtraction
{
    private static readonly Calendar _calendar = CultureInfo.InvariantCulture.Calendar;
    private static readonly CalendarWeekRule _weekRule = CalendarWeekRule.FirstFourDayWeek;
    private static readonly DayOfWeek _firstDayOfWeek = DayOfWeek.Monday;

    /// <summary>
    /// Extracts the ISO week, ISO year, and lagged week, lagged year (current week - 2 weeks)
    /// </summary>
    /// <param name="date">The date to extract information from</param>
    /// <returns>Tuple of (ISOWeek, ISOYear, LaggedWeek, LaggedYear)</returns>
    public (int ISOWeek, int ISOYear, int LaggedWeek, int LaggedYear) ExtractCurrentDateAndLaggedDate(DateOnly date)
    {
        // Get ISO week and year for current date
        var currentDateTime = date.ToDateTime(TimeOnly.MinValue);
        int currentISOWeek = _calendar.GetWeekOfYear(currentDateTime, _weekRule, _firstDayOfWeek);
        int currentISOYear = GetISOYear(currentDateTime, currentISOWeek);

        // Calculate lagged date (2 weeks prior)
        var laggedDate = date.AddDays(-14);
        var laggedDateTime = laggedDate.ToDateTime(TimeOnly.MinValue);
        
        // Get ISO week and year for lagged date
        int laggedISOWeek = _calendar.GetWeekOfYear(laggedDateTime, _weekRule, _firstDayOfWeek);
        int laggedISOYear = GetISOYear(laggedDateTime, laggedISOWeek);

        return (currentISOWeek, currentISOYear, laggedISOWeek, laggedISOYear);
    }

    /// <summary>
    /// Overload that accepts DateTime instead of DateOnly
    /// </summary>
    public (int ISOWeek, int ISOYear, int LaggedWeek, int LaggedYear) ExtractCurrentDateAndLaggedDate(DateTime date)
    {
        return ExtractCurrentDateAndLaggedDate(DateOnly.FromDateTime(date));
    }

    /// <summary>
    /// Determines the ISO year for a given date and week number
    /// The ISO year can differ from the calendar year at year boundaries
    /// </summary>
    private int GetISOYear(DateTime date, int weekNumber)
    {
        int year = date.Year;
        
        // If we're in week 52 or 53 but in December, and the date is in the last few days,
        // check if it belongs to the next year
        if (weekNumber >= 52 && date.Month == 12 && date.Day >= 29)
        {
            return year;
        }
        
        // If we're in week 1 but in January, and it's early in the month,
        // the week might belong to the previous year
        if (weekNumber == 1 && date.Month == 1 && date.Day <= 3)
        {
            return year;
        }
        
        // If we're in week 52 or 53 in early January, it belongs to the previous year
        if (weekNumber >= 52 && date.Month == 1)
        {
            return year - 1;
        }
        
        return year;
    }
}