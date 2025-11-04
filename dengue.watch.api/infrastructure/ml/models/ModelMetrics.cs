namespace dengue.watch.api.infrastructure.ml.models;

/// <summary>
/// Model training metrics
/// </summary>
public record ModelMetrics(
    double Accuracy,
    double MeanAbsoluteError,
    double RSquared,
    DateTime TrainedAt,
    int TrainingDataSize
);