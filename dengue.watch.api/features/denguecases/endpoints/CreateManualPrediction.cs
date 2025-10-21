//
// namespace dengue.watch.api.features.denguecases.endpoints;
//
// public class CreateManualPredictionByPsgcCode : IEndpoint
// {
//     public static IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder app)
//     {
//         var group = app.MapGroup("api/dengue-cases")
//             .WithSummary("Manual Predict Dengue Case by PSGC Code")
//             .WithName("ManualPredictDengueCaseByPsgcCode")
//             .WithTags("Dengue Cases");
//
//         group.MapPost("/predict/{psgccode}", HandleAsync);
//         return group;
//     }
//
//     public record PredictDengueCaseRequestByPsgcCode( DateOnly? startDate, DateOnly? endDate, bool withDate = false);
//
//     public record PredicDengueCaseResponse(string psgccode, string barangayName, int laggedISOWeek, int laggedYear, float predictionValue);
//     
//     private static Task HandleAsync(
//         string psgccode,
//         [FromBody]PredictDengueCaseRequestByPsgcCode predictDengueCaseRequestByDate,
//         ILogger<CreateManualPredictionByPsgcCode> _logger,
//         ApplicationDbContext _dbContext)
//     {
//         _logger.LogInformation("PredictDengueCaseRequestByPsgcCode");
//         
//         
//         
//         
//         throw new NotImplementedException();
//     }
// }