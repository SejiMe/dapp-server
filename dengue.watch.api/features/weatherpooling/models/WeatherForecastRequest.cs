namespace dengue.watch.api.features.weatherpooling.models;

public record WeatherForecastRequest(
    string PsgcCode,
    decimal Latitude,
    decimal Longitude
);
