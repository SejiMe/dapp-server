namespace dengue.watch.api.features.denguecases.dtos;

/// <summary>
/// Response model for a single weekly dengue case
/// </summary>
public record WeeklyDengueCaseResponse(
    long Id,
    string PsgcCode,
    string BarangayName,
    int Year,
    int WeekNumber,
    int CaseCount
);

/// <summary>
/// Response model for list of weekly dengue cases
/// </summary>
public record WeeklyDengueCasesListResponse(
    int PageNumber,
    int PageSize,
    int TotalCount,
    List<WeeklyDengueCaseResponse> Cases
);

/// <summary>
/// Request model for creating a new weekly dengue case
/// </summary>
public record CreateWeeklyDengueCaseRequest(
    string PsgcCode,
    int Year,
    int WeekNumber,
    int CaseCount
);

/// <summary>
/// Request model for updating a weekly dengue case
/// </summary>
public record UpdateWeeklyDengueCaseRequest(
    int? Year = null,
    int? WeekNumber = null,
    int? CaseCount = null
);
