using Microsoft.ML.Data;

namespace dengue.watch.api.infrastructure.ml.models;

/// <summary>
/// Input data for dengue forecasting
/// </summary>

public class AdvDengueForecastInput
{
    
  
        // Put all your dataset fields here. Use attributes to map if loading from CSV.

        [LoadColumn(0)]
        public string PsgcCode { get; set; }
        
        [LoadColumn(1)]
        public int DengueYear { get; set; }

        [LoadColumn(2)]
        public int DengueWeekNumber { get; set; }

        [LoadColumn(3)]
        [ColumnName("DengueCount")]
        public float DengueCaseCount { get; set; }  // Label

        // Lag features...
        [LoadColumn(4)]
        public int LagYear { get; set; }
        [LoadColumn(5)]
        public int LagWeekNumber { get; set; }

        // Weather features:
        [LoadColumn(7)]
        public float TemperatureMean { get; set; }
        [LoadColumn(8)]
        public float TemperatureMax { get; set; }
        [LoadColumn(9)]
        public float HumidityMean { get; set; }
        [LoadColumn(10)]
        public float HumidityMax { get; set; }
        [LoadColumn(11)]
        public float PrecipitationMean { get; set; }
        [LoadColumn(12)]
        public float PrecipitationMax { get; set; }

        // You might also include DominantWeatherCategory etc if relevant
        [LoadColumn(16)]
        public string DominantWeatherCategory { get; set; }
        
        [LoadColumn(17)]
        public string IsWetWeek { get; set; }
}