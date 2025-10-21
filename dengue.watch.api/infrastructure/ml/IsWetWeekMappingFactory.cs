using Microsoft.ML.Transforms;

namespace dengue.watch.api.infrastructure.ml;


// Input/Output classes
public class IsWetWeekInput
{
    public string IsWetWeek { get; set; }
}

public class IsWetWeekTransformed
{
    public float IsWetWeekFloat { get; set; }
}


[CustomMappingFactoryAttribute("IsWetWeekConversion")]
public class IsWetWeekMappingFactory : CustomMappingFactory<IsWetWeekInput, IsWetWeekTransformed>
{
    public override Action<IsWetWeekInput, IsWetWeekTransformed> GetMapping()
    {
        return (input, output) =>
        {
            output.IsWetWeekFloat = input.IsWetWeek?.ToUpper() == "TRUE" ? 1.0f : 0.0f;
        };
    }
}
