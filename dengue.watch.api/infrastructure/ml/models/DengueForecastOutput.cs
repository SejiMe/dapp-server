using Microsoft.ML.Data;
namespace dengue.watch.api.infrastructure.ml.models;


/// <summary>
/// Output prediction for dengue forecasting
/// </summary>
public class DengueForecastOutput
{
    [ColumnName("Score")]
    public float Score { get; set; } // Predicted dengue cases
    
    // NEW: Add these properties for probability and confidence intervals
    public float LowerBound { get; set; }
    public float UpperBound { get; set; }
    public double ConfidencePercentage { get; set; }
    public double ProbabilityOfOutbreak { get; set; }
    
    // Optional: Add interpretation helper
    public string GetRiskLevel()
    {
        if (ProbabilityOfOutbreak >= 70)
            return "High Risk";
        else if (ProbabilityOfOutbreak >= 40)
            return "Moderate Risk";
        else
            return "Low Risk";
    }
    
    public string GetPredictionSummary()
    {
        return $"Predicted: {Score:F1} cases\n" +
               $"Expected Range: {LowerBound:F1} - {UpperBound:F1} cases ({ConfidencePercentage:F0}% confidence)\n" +
               $"Outbreak Probability: {ProbabilityOfOutbreak:F1}%\n" +
               $"Risk Level: {GetRiskLevel()}";
    }
}
// public class DengueForecastOutput
// {
//     [ColumnName("Score")]
//     public float PredictedCaseCount { get; set; }
// }



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