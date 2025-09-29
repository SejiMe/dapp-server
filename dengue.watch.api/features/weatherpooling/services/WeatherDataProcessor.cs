using dengue.watch.api.features.weatherpooling.models;
using Google.FlatBuffers;
using openmeteo_sdk;

namespace dengue.watch.api.features.weatherpooling.services;

public class WeatherDataProcessor
{

    public DailyWeatherData Get1DayData(WeatherHistoricalResponse apiResponse)
    {
        if (apiResponse == null || apiResponse.Daily == null)
        {
            throw new NotFoundException("Daily data not found");
        }

        var daily = apiResponse.Daily;
        if (daily.Time == null || daily.Time.Count == 0)
        {
            throw new NotFoundException("Daily dates not found");
        }

        // Extract first item (day minus 1) across parallel arrays
        var index = 0;

        var result = new DailyWeatherData
        {
            Date = daily.Time[index],
            WeatherCode = daily.WeatherCode != null && daily.WeatherCode.Count > index ? daily.WeatherCode[index] : 0,
            PrecipitationSum = daily.PrecipitationSum != null && daily.PrecipitationSum.Count > index ? daily.PrecipitationSum[index] : 0,
            PrecipitationHours = daily.PrecipitationHours != null && daily.PrecipitationHours.Count > index ? daily.PrecipitationHours[index] : 0,
            RelativeHumidityMean = daily.RelativeHumidity2mMean != null && daily.RelativeHumidity2mMean.Count > index ? daily.RelativeHumidity2mMean[index] : 0,
            TemperatureMean = daily.Temperature2mMean != null && daily.Temperature2mMean.Count > index ? daily.Temperature2mMean[index] : 0
        };

        return result;
    }

}