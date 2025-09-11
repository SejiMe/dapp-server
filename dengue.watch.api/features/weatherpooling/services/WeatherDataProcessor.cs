using dengue.watch.api.features.weatherpooling.models;
using Google.FlatBuffers;
using openmeteo_sdk;

namespace dengue.watch.api.features.weatherpooling.services;

public class WeatherDataProcessor : IDisposable
{

    private readonly ILogger<WeatherDataProcessor> _logger;

    public void Dispose()
    {
    }

    public WeatherDataProcessor(ILogger<WeatherDataProcessor> logger)
    {
        _logger = logger;
    }
    public VariablesWithTime? GetDailyPastDay1Data(ByteBuffer flatBufferData)
    {
        
        // var weatherResponse = WeatherApiResponse.GetRootAsWeatherApiResponse(byteBuffer);
        var isValidResponse = WeatherApiResponse.VerifyWeatherApiResponse(flatBufferData);

        if(!isValidResponse)
        {
            throw new NotFoundException("Daily data not found");
        }

        var weatherResponse = WeatherApiResponse.GetRootAsWeatherApiResponse(flatBufferData);

        if (!weatherResponse.Daily.HasValue)
        {
            throw new NotFoundException("Daily data not found");
        }

    var dailyData = weatherResponse.Daily;

        return dailyData;
        
    }
    
    public DailyWeatherData GetDayMinus1Data(WeatherForecastResponse apiResponse)
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

     private List<DailyWeatherData> ProcessDailyData(DailyWeatherData daily)
    {
        var dailyWeatherList = new List<DailyWeatherData>();


        // for (int i = 0; i < timeCount; i++)
        // {
        //     var timestamp = daily.Time(i);
        //     var date = DateTimeOffset.FromUnixTimeSeconds(timestamp).Date;

        //     var weatherData = new DailyWeatherData
        //     {
        //         Date = date
        //     };

        //     // Map variables based on the order in your URL
        //     // daily=weather_code,precipitation_sum,precipitation_hours,rain_sum,relative_humidity_2m_mean,temperature_2m_mean
        //     for (int varIndex = 0; varIndex < daily.VariablesLength; varIndex++)
        //     {
        //         var variable = daily.Variables(varIndex);
        //         if (!variable.HasValue) continue;

        //         var value = variable.Value.Values(i);

        //         // Map based on the order in your API call
        //         switch (varIndex)
        //         {
        //             case 0: // weather_code
        //                 weatherData.WeatherCode = (int)value;
        //                 break;
        //             case 1: // precipitation_sum
        //                 weatherData.PrecipitationSum = value;
        //                 break;
        //             case 2: // precipitation_hours
        //                 weatherData.PrecipitationHours = value;
        //                 break;
        //             case 3: // rain_sum
        //                 weatherData.RainSum = value;
        //                 break;
        //             case 4: // relative_humidity_2m_mean
        //                 weatherData.RelativeHumidityMean = value;
        //                 break;
        //             case 5: // temperature_2m_mean
        //                 weatherData.TemperatureMean = value;
        //                 break;
        //         }
        //     }

        //     dailyWeatherList.Add(weatherData);
        // }

        return dailyWeatherList;
    }
}