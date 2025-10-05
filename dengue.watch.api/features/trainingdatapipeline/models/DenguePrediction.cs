using Microsoft.ML.Data;
namespace dengue.watch.api.features.trainingdatapipeline.models;

public class DenguePrediction
{
    [ColumnName("Score")]
    public float PredictedCaseCount { get; set; }
}