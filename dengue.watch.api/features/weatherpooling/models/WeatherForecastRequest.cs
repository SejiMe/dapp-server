namespace dengue.watch.api.features.weatherpooling.models;

public record WeatherHistoricalRequest(
    string PsgcCode,
    decimal Latitude,
    decimal Longitude
);



public record WeatherHistoricalLongRequest(
    string PsgcCode,
    decimal Latitude,
    decimal Longitude,
    DateTime StartDate,
    DateTime EndDate
);




