namespace dengue.watch.api.features.weatherpooling.models;

public class DailyWeatherData
{
    
    public string FK_PsgcCode { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public int WeatherCode { get; set; }
    public double PrecipitationSum { get; set; }
    public double PrecipitationHours { get; set; }
    public double RelativeHumidityMean { get; set; }
    public double TemperatureMean { get; set; }
}