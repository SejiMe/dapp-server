using Microsoft.ML.Data;
namespace dengue.watch.api.infrastructure.ml.models;


/// <summary>
/// Output prediction for dengue forecasting
/// </summary>
public class DengueForecastOutput
{
    [ColumnName("Score")]
    public float PredictedCaseCount { get; set; }
}



// public class DengueForecastOutput
// { 
//     // Forecast for next 7 days
//     public float ForecastedCases { get; set; } 
//     public float[] LowerBoundCases { get; set; } 
//     public float[] UpperBoundCases { get; set; } 
//     
//     public int IsoWeek { get; set; }
//     public int IsoYear { get; set; }
//     public int LaggedIsoWeek { get; set; }
//     public int LaggedIsoYear { get; set; }
//     public float ConfidenceLevel { get; set; }
// }