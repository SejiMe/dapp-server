

namespace dengue.watch.api.features.denguecases.dtos;

public class HistoricalDengueCases
{
    public string psgccode { get; set; }
    public List<YearlyTotalDengueCase> TotalDengueCases { get; set; }
}

public record YearlyTotalDengueCase(string year,  int totalCases);
