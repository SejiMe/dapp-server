namespace dengue.watch.api.features.administrativeareas;

public record AdministrativeAreaDto(
    string PsgcCode,
    string Name,
    string GeographicLevel,
    decimal? Latitude,
    decimal? Longitude
);


