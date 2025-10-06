namespace dengue.watch.api.features.denguecases.services;


public interface IPredictDengueCaseService
{
    public void PredictWeeklyData();
}

public class PredictDengueCaseService : IPredictDengueCaseService
{
    // This either throw an error or Logs a success
    // This will only be run
    public void PredictWeeklyData()
    {

        throw new NotImplementedException();
    }
    

}