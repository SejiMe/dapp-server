using dengue.watch.api.infrastructure.database;
using Microsoft.EntityFrameworkCore;

namespace dengue.watch.api.features.administrativeareas;

public interface IAdministrativeAreaService
{
    Task<IEnumerable<AdministrativeAreaDto>> GetRegionsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<AdministrativeAreaDto>> GetProvincesAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<AdministrativeAreaDto>> GetProvincesByRegionAsync(string regionPsgcCode, CancellationToken cancellationToken = default);
    Task<IEnumerable<AdministrativeAreaDto>> GetCitiesAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<AdministrativeAreaDto>> GetCitiesByProvinceAsync(string provincePsgcCode, CancellationToken cancellationToken = default);
    Task<IEnumerable<AdministrativeAreaDto>> GetMunicipalitiesAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<AdministrativeAreaDto>> GetMunicipalitiesByProvinceAsync(string provincePsgcCode, CancellationToken cancellationToken = default);
    Task<IEnumerable<AdministrativeAreaDto>> GetBarangaysAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<AdministrativeAreaDto>> GetBarangaysByCityOrMunicipalityAsync(string cityOrMunicipalityPsgcCode, CancellationToken cancellationToken = default);
}

public class AdministrativeAreaService : IAdministrativeAreaService
{
    private readonly ApplicationDbContext _context;

    public AdministrativeAreaService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<AdministrativeAreaDto>> GetRegionsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.AdministrativeAreas
            .Where(a => a.GeographicLevel.ToLower() == "reg")
            .OrderBy(a => a.PsgcCode)
            .Select(a => new AdministrativeAreaDto(a.PsgcCode, a.Name, a.GeographicLevel, a.Latitude, a.Longitude))
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AdministrativeAreaDto>> GetProvincesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.AdministrativeAreas
            .Where(a => a.GeographicLevel.ToLower() == "prov")
            .OrderBy(a => a.PsgcCode)
            .Select(a => new AdministrativeAreaDto(a.PsgcCode, a.Name, a.GeographicLevel, a.Latitude, a.Longitude))
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AdministrativeAreaDto>> GetProvincesByRegionAsync(string regionPsgcCode, CancellationToken cancellationToken = default)
    {
        var regionPrefix = GetRegionPrefix(regionPsgcCode);
        return await _context.AdministrativeAreas
            .Where(a => a.GeographicLevel.ToLower() == "prov" && a.PsgcCode.StartsWith(regionPrefix))
            .OrderBy(a => a.PsgcCode)
            .Select(a => new AdministrativeAreaDto(a.PsgcCode, a.Name, a.GeographicLevel, a.Latitude, a.Longitude))
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AdministrativeAreaDto>> GetCitiesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.AdministrativeAreas
            .Where(a => a.GeographicLevel.ToLower() == "city")
            .OrderBy(a => a.PsgcCode)
            .Select(a => new AdministrativeAreaDto(a.PsgcCode, a.Name, a.GeographicLevel, a.Latitude, a.Longitude))
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AdministrativeAreaDto>> GetCitiesByProvinceAsync(string provincePsgcCode, CancellationToken cancellationToken = default)
    {
        var provincePrefix = GetProvincePrefix(provincePsgcCode);
        return await _context.AdministrativeAreas
            .Where(a => a.GeographicLevel.ToLower() == "city" && a.PsgcCode.StartsWith(provincePrefix))
            .OrderBy(a => a.PsgcCode)
            .Select(a => new AdministrativeAreaDto(a.PsgcCode, a.Name, a.GeographicLevel, a.Latitude, a.Longitude))
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AdministrativeAreaDto>> GetMunicipalitiesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.AdministrativeAreas
            .Where(a => a.GeographicLevel.ToLower() == "municipality")
            .OrderBy(a => a.PsgcCode)
            .Select(a => new AdministrativeAreaDto(a.PsgcCode, a.Name, a.GeographicLevel, a.Latitude, a.Longitude))
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AdministrativeAreaDto>> GetMunicipalitiesByProvinceAsync(string provincePsgcCode, CancellationToken cancellationToken = default)
    {
        var provincePrefix = GetProvincePrefix(provincePsgcCode);
        return await _context.AdministrativeAreas
            .Where(a => a.GeographicLevel.ToLower() == "municipality" && a.PsgcCode.StartsWith(provincePrefix))
            .OrderBy(a => a.PsgcCode)
            .Select(a => new AdministrativeAreaDto(a.PsgcCode, a.Name, a.GeographicLevel, a.Latitude, a.Longitude))
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AdministrativeAreaDto>> GetBarangaysAsync(CancellationToken cancellationToken = default)
    {
        return await _context.AdministrativeAreas
            .Where(a => a.GeographicLevel.ToLower() == "bgy")
            .OrderBy(a => a.PsgcCode)
            .Select(a => new AdministrativeAreaDto(a.PsgcCode, a.Name, a.GeographicLevel, a.Latitude, a.Longitude))
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AdministrativeAreaDto>> GetBarangaysByCityOrMunicipalityAsync(string cityOrMunicipalityPsgcCode, CancellationToken cancellationToken = default)
    {
        var cityOrMunicipalityPrefix = GetCityOrMunicipalityPrefix(cityOrMunicipalityPsgcCode);
        return await _context.AdministrativeAreas
            .Where(a => a.GeographicLevel.ToLower() == "bgy" && a.PsgcCode.StartsWith(cityOrMunicipalityPrefix))
            .OrderBy(a => a.PsgcCode)
            .Select(a => new AdministrativeAreaDto(a.PsgcCode, a.Name, a.GeographicLevel, a.Latitude, a.Longitude ))
            .ToListAsync(cancellationToken);
    }

    private static string GetRegionPrefix(string psgc)
    {
        // First 2 digits are region
        return psgc[..2];
    }

    private static string GetProvincePrefix(string psgc)
    {
        // First 5 digits are province (2 region + 3 province)
        return psgc[..5];
    }

    private static string GetCityOrMunicipalityPrefix(string psgc)
    {
        // First 7 digits are city/municipality (2 region + 3 province + 2 city/municipality)
        return psgc[..7];
    }
}


