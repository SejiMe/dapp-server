using Microsoft.ML.Data;

namespace dengue.watch.api.infrastructure.ml.models;
public class LaggedDengueCausalData
    {
        // Put all your dataset fields here. Use attributes to map if loading from CSV.

        [LoadColumn(0)]
        public int DengueYear { get; set; }

        [LoadColumn(1)]
        public int DengueWeekNumber { get; set; }

        [LoadColumn(2)]
        [ColumnName("Label")]
        public float DengueCaseCount { get; set; }  // Label

        // Lag features...
        [LoadColumn(3)]
        public int LagYear { get; set; }
        [LoadColumn(4)]
        public int LagWeekNumber { get; set; }

        // Weather features:
        [LoadColumn(6)]
        public float TemperatureMean { get; set; }
        [LoadColumn(7)]
        public float TemperatureMax { get; set; }
        [LoadColumn(8)]
        public float HumidityMean { get; set; }
        [LoadColumn(9)]
        public float HumidityMax { get; set; }
        [LoadColumn(10)]
        public float PrecipitationMean { get; set; }
        [LoadColumn(11)]
        public float PrecipitationMax { get; set; }

        // You might also include DominantWeatherCategory etc if relevant
        [LoadColumn(15)]
        public string DominantWeatherCategory { get; set; }



        [LoadColumn(16)]
        public string IsWetWeek { get; set; }
    }



/* 
0DengueYear 
 1   ,DengueWeekNumber
  2  ,DengueCaseCount
   3 ,LagYear
    4,LagWeekNumber
   5 ,LagWeekStartDat // skip
   6 e, TemperatureMean
   7 , TemperatureMax
   8 , HumidityMean
   9 , HumidityMax
10 , PrecipitationMean
   11 , PrecipitationMax,
     12, MostCommonWeatherCodeId,
     13, MostCommonWeatherDescription,
       14, OccurrenceCount,
        15, DominantWeatherCategory,
         IsWe */