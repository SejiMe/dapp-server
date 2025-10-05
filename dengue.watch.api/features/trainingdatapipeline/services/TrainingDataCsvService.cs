using System.Globalization;
using System.Text;
using dengue.watch.api.features.trainingdatapipeline.models;

namespace dengue.watch.api.features.trainingdatapipeline.services;

public interface ITrainingDataCsvService
{
    TrainingDataCsvFileResult CreateCsv(WeeklyTrainingWeatherResult weeklyResult, bool isPgscCodeExcluded);
}

public sealed class TrainingDataCsvService : ITrainingDataCsvService
{
    private const string ContentType = "text/csv";
    private const string DateFormat = "MM-dd-yyyy";
    private const string DefaultFileName = "weekly-training-data.csv";
    private const string DecimalFormat = "{0:F2}";
    private static string[] HeaderColumns =
    {
        "PsgcCode",
        "DengueYear",
        "DengueWeekNumber",
        "DengueCaseCount",
        "LagYear",
        "LagWeekNumber",
        "LagWeekStartDate",
        "TemperatureMean",
        "TemperatureMax",
        "HumidityMean",
        "HumidityMax",
        "PrecipitationMean",
        "PrecipitationMax",
        "MostCommonWeatherCodeId",
        "MostCommonWeatherDescription",
        "OccurrenceCount",
        "DominantWeatherCategory",
        "IsWetWeek"
    };

    public TrainingDataCsvFileResult CreateCsv(WeeklyTrainingWeatherResult weeklyResult, bool isPgscCodeExcluded)
    {
        ArgumentNullException.ThrowIfNull(weeklyResult);
        
        var builder = new StringBuilder();

        WriteHeader(builder, isPgscCodeExcluded);

        foreach (var snapshot in weeklyResult.Snapshots)
        {
            WriteSnapshotLine(builder, snapshot, isPgscCodeExcluded);
        }

        if (weeklyResult.MissingLagWeeks.Count > 0)
        {
            WriteSeparator(builder);
            WriteMissingLagWeeksHeader(builder);

            foreach (var missing in weeklyResult.MissingLagWeeks)
            {
                builder.AppendLine(EscapeCsvValue(missing));
            }
        }

        var content = Encoding.UTF8.GetBytes(builder.ToString());

        return new TrainingDataCsvFileResult(DefaultFileName, ContentType, content);
    }

    private static void WriteHeader(StringBuilder builder, bool isPgscCodeExcluded)
    {
        builder.AppendLine(string.Join(',',  isPgscCodeExcluded ? HeaderColumns[1..] : HeaderColumns));
    }

    private static void WriteSnapshotLine(StringBuilder builder, WeeklyTrainingWeatherSnapshot snapshot, bool isPgscCodeExcluded)
    {
        if (!isPgscCodeExcluded)
            builder.Append(EscapeCsvValue(snapshot.PsgcCode)).Append(',');

        builder
            .Append(snapshot.DengueYear).Append(',')
            .Append(snapshot.DengueWeekNumber).Append(',')
            .Append(snapshot.DengueCaseCount).Append(',')
            .Append(snapshot.LagYear).Append(',')
            .Append(snapshot.LagWeekNumber).Append(',')
            .Append(snapshot.LagWeekStartDate.ToString(DateFormat, CultureInfo.InvariantCulture)).Append(',')
            .AppendFormat(CultureInfo.InvariantCulture, DecimalFormat, snapshot.Temperature.Mean).Append(',')
            .AppendFormat(CultureInfo.InvariantCulture, DecimalFormat, snapshot.Temperature.Max).Append(',')
            .AppendFormat(CultureInfo.InvariantCulture, DecimalFormat, snapshot.Humidity.Mean).Append(',')
            .AppendFormat(CultureInfo.InvariantCulture, DecimalFormat, snapshot.Humidity.Max).Append(',')
            .AppendFormat(CultureInfo.InvariantCulture, DecimalFormat, snapshot.Precipitation.Mean).Append(',')
            .AppendFormat(CultureInfo.InvariantCulture, DecimalFormat, snapshot.Precipitation.Max).Append(',')
            .Append(snapshot.MostCommonWeatherCodeId).Append(',')
            .Append(EscapeCsvValue(snapshot.MostCommonWeatherDescription)).Append(',')
            .Append(snapshot.OccurrenceCount).Append(',')
            .Append(EscapeCsvValue(snapshot.DominantWeatherCategory)).Append(',')
            .Append(snapshot.IsWetWeek ? "TRUE" : "FALSE")
            .AppendLine();
    }

    private static void WriteSeparator(StringBuilder builder)
    {
        builder.AppendLine().AppendLine("Missing Lagged Weeks");
    }

    private static void WriteMissingLagWeeksHeader(StringBuilder builder)
    {
        builder.AppendLine("LagWeek");
    }

    private static string EscapeCsvValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var needsEscaping = value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r');
        if (!needsEscaping)
        {
            return value;
        }

        var escaped = value.Replace("\"", "\"\"");
        return $"\"{escaped}\"";
    }
}
