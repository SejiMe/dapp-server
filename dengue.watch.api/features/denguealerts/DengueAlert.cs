namespace dengue.watch.api.features.denguealerts;

/// <summary>
/// Dengue alert entity
/// </summary>
public class DengueAlert
{
    public int Id { get; set; }
    public string Location { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public AlertLevel Level { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Alert level enumeration
/// </summary>
public enum AlertLevel
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}
