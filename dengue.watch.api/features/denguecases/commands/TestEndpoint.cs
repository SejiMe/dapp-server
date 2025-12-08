namespace dengue.watch.api.features.denguecases.commands
{
    public class BulkUpdateEndpoint : IEndpoint
    {
        public static IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder app)
        {
            app.MapPost("bulk-update-months", Handler);

            return app;
        }

        private static async Task<IResult> Handler([FromServices] ApplicationDbContext _db)
        {
            var records = _db.PredictedWeeklyDengues.Where(p => p.MonthName == null).ToList();

            foreach (var record in records)
            {
                record.MonthName = IsoWeekHelper.GetMonthNameFromIsoWeek(
                record.PredictedIsoYear, 
                record.PredictedIsoWeek);
            }
            await _db.SaveChangesAsync();
            return Results.Ok($"Updated {records.Count} records");
        }
    }
}