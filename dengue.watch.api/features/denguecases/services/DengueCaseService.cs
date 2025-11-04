// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Threading.Tasks;
// using dengue.watch.api.features.denguecases.dtos;

// namespace dengue.watch.api.features.denguecases.services
// {
//     public class DengueCaseService : IMonthlyCensus
//     {
//         private readonly ILogger<DengueCaseService> _logger;
//         private readonly ApplicationDbContext _db;
//         private readonly DateExtraction _dtExtraction;
//         public DengueCaseService(ApplicationDbContext dbContext, DateExtraction dt, ILogger<DengueCaseService> logger)
//         {
//             _db = dbContext;
//             _dtExtraction = dt;
//             _logger = logger;
//         }
//         public Task<MonthlyCensusResponse2> MonthlyCensusByPsgcAndYear(string psgccode, int year)
//         {
//             var dateParts = _dtExtraction.ExtractCurrentDateAndLaggedDate(new DateOnly(1, 1, year));

//             int currentMonth = 1;
//             MonthlyCensusResponse2 results = new();
//             while (currentMonth <= 12)
//             {

//                 WeeklyData
//                 results.weekly_census_list.Add();
//                 currentMonth++;
//             }

//         }
//     }
// }