namespace dengue.watch.api.features.weathersummary.models;

public record GetWeatherSummaryRequest(string PsgcCode, DateOnly DateSelected);