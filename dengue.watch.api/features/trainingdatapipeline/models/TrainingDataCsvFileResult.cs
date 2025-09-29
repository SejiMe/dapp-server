namespace dengue.watch.api.features.trainingdatapipeline.models;

public sealed record TrainingDataCsvFileResult(
    string FileName,
    string ContentType,
    byte[] Content);
