using Microsoft.ML.Data;
namespace dengue.watch.api.infrastructure.ml.models;

public class DenguePrediction
{
    [ColumnName("Score")]
    public float PredictedCaseCount { get; set; }
}