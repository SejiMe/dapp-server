namespace dengue.watch.api.infrastructure.ml.models;

/// <summary>
/// Model information
/// </summary>
public record ModelInfo(
    string Name,
    string Version,
    DateTime LastTrained,
    bool IsLoaded,
    string Description
);
