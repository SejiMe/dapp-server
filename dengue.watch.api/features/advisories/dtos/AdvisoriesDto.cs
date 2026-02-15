using dengue.watch.api.infrastructure.database;

namespace dengue.watch.api.features.advisories.dtos;

/// <summary>
/// Response model for a single community advisory
/// </summary>
public record CommunityAdvisoryResponse(
    Guid Id,
    string Title,
    string Description,
    string ActionPlan,
    string RiskLevel,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string? CreatedBy,
    bool IsActive
);

/// <summary>
/// Response model for list of advisories
/// </summary>
public record AdvisoriesListResponse(
    int PageNumber,
    int PageSize,
    int TotalCount,
    List<CommunityAdvisoryResponse> Advisories
);

/// <summary>
/// Request model for creating a new advisory
/// </summary>
public record CreateAdvisoryRequest(
    string Title,
    string Description,
    string ActionPlan,
    RiskLevel RiskLevel
);

/// <summary>
/// Request model for updating an advisory
/// </summary>
public record UpdateAdvisoryRequest(
    string? Title = null,
    string? Description = null,
    string? ActionPlan = null,
    RiskLevel? RiskLevel = null,
    bool? IsActive = null
);
