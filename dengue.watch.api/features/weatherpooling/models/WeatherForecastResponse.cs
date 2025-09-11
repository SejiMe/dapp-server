namespace dengue.watch.api.features.weatherpooling.models;

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

public class WeatherForecastResponse
{
    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }

    [JsonPropertyName("generationtime_ms")]
    public double GenerationTimeMs { get; set; }

    [JsonPropertyName("utc_offset_seconds")]
    public int UtcOffsetSeconds { get; set; }

    [JsonPropertyName("timezone")]
    public string Timezone { get; set; }

    [JsonPropertyName("timezone_abbreviation")]
    public string TimezoneAbbreviation { get; set; }

    [JsonPropertyName("elevation")]
    public double Elevation { get; set; }

    [JsonPropertyName("daily_units")]
    public DailyUnits DailyUnits { get; set; }

    [JsonPropertyName("daily")]
    public Daily Daily { get; set; }
}

public class DailyUnits
{
    [JsonPropertyName("time")]
    public string Time { get; set; }

    [JsonPropertyName("weather_code")]
    public string WeatherCode { get; set; }

    [JsonPropertyName("precipitation_sum")]
    public string PrecipitationSum { get; set; }

    [JsonPropertyName("precipitation_hours")]
    public string PrecipitationHours { get; set; }

    [JsonPropertyName("rain_sum")]
    public string RainSum { get; set; }

    [JsonPropertyName("relative_humidity_2m_mean")]
    public string RelativeHumidity2mMean { get; set; }

    [JsonPropertyName("temperature_2m_mean")]
    public string Temperature2mMean { get; set; }
}

public class Daily
{
    [JsonPropertyName("time")]
    public List<DateTime> Time { get; set; }

    [JsonPropertyName("weather_code")]
    public List<int> WeatherCode { get; set; }

    [JsonPropertyName("precipitation_sum")]
    public List<double> PrecipitationSum { get; set; }

    [JsonPropertyName("precipitation_hours")]
    public List<double> PrecipitationHours { get; set; }

    [JsonPropertyName("rain_sum")]
    public List<double> RainSum { get; set; }

    [JsonPropertyName("relative_humidity_2m_mean")]
    public List<int> RelativeHumidity2mMean { get; set; }

    [JsonPropertyName("temperature_2m_mean")]
    public List<double> Temperature2mMean { get; set; }
}
