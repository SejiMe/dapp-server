using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace dengue.watch.api.common.helpers
{
    public static class IsoWeekHelper
    {
        /// <summary>
        /// Gets the month name from an ISO week number for a given year.
        /// Returns the month of the Thursday in that ISO week (ISO 8601 standard).
        /// </summary>
        /// <param name="year">The year</param>
        /// <param name="isoWeek">The ISO week number (1-53)</param>
        /// <param name="culture">Optional culture info for month name localization</param>
        /// <returns>The month name</returns>
        public static string GetMonthNameFromIsoWeek(int year, int isoWeek, CultureInfo culture = null)
        {
            if (isoWeek < 1 || isoWeek > 53)
            {
                throw new ArgumentOutOfRangeException(nameof(isoWeek), "ISO week must be between 1 and 53");
            }

            culture ??= CultureInfo.CurrentCulture;

            // Get the first day of the ISO week
            DateTime isoWeekDate = ISOWeek.ToDateTime(year, isoWeek, DayOfWeek.Monday);
            
            // ISO weeks are defined by the Thursday they contain
            DateTime thursday = isoWeekDate.AddDays(3);
            
            return thursday.ToString("MMM", culture).ToUpper();
        }
    }
}