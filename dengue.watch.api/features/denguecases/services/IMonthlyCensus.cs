using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using dengue.watch.api.features.denguecases.dtos;

namespace dengue.watch.api.features.denguecases.services
{
    public interface IMonthlyCensus
    {
        Task<MonthlyCensusResponse2> MonthlyCensusByPsgcAndYear(string psgccode, int year);
        
    }
}