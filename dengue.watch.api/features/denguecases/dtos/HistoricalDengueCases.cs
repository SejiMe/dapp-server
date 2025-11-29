

namespace dengue.watch.api.features.denguecases.dtos;

public class HistoricalYearlyDengueCases
{
    public string? psgccode { get; set; } 
    public List<YearlyTotalDengueCase> recorded_cases { get; set; }
}

public record YearlyTotalDengueCase(string year,  int total_cases);



public record HistoricalWeeklyDengueCases(int year, int week, int recorded_cases);

