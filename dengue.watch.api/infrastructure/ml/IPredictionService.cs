namespace dengue.watch.api.infrastructure.ml;

/// <summary>
/// Interface for ML prediction services
/// </summary>
public interface IPredictionService<TInput, TOutput>
    where TInput : class
    where TOutput : class
{
    /// <summary>
    /// Make a single prediction
    /// </summary>
    /// <param name="input">Input data for prediction</param>
    /// <returns>Prediction result</returns>
    Task<TOutput> PredictAsync(TInput input);

    /// <summary>
    /// Make batch predictions
    /// </summary>
    /// <param name="inputs">Input data for predictions</param>
    /// <returns>Prediction results</returns>
    Task<IEnumerable<TOutput>> PredictBatchAsync(IEnumerable<TInput> inputs);

    /// <summary>
    /// Train or retrain the model
    /// </summary>
    /// <param name="trainingData">Training data</param>
    /// <returns>Training metrics</returns>
    Task<ModelMetrics> TrainModelAsync();
    
    /// <summary>
    /// Get model information
    /// </summary>
    /// <returns>Model information</returns>
    ModelInfo GetModelInfo();
    
}



