using Facet;

namespace dengue.watch.api.features.denguealerts;

/// <summary>
/// Dengue alert DTO using Facet for auto-mapping
/// </summary>
[Facet(typeof(DengueAlert))]
public partial record DengueAlertDto
{
    // Facet will automatically generate all properties from DengueAlert
    // Additional properties can be added here if needed
}

/// <summary>
/// Create dengue alert request DTO
/// </summary>
public record CreateDengueAlertRequest(
    string Location,
    string Description,
    AlertLevel Level
);

/// <summary>
/// Update dengue alert request DTO
/// </summary>
public record UpdateDengueAlertRequest(
    string? Location,
    string? Description,
    AlertLevel? Level,
    bool? IsActive
);
