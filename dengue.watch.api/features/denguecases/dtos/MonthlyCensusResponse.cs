using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace dengue.watch.api.features.denguecases.dtos
{
    public class MonthlyCensusResponse
    {
        public string month_name { get; set; }

        public float probability { get; set; }
        public int week { get; set; }
        public int year { get; set; }
        public int case_count { get; set; }
    }

    public class MonthlyCensusResponse2
    {
        public string month_name { get; set; }

        public List<WeeklyData> weekly_census_list = new();
    }
    
    
    public class WeeklyData
    {
        public float probability { get; set; }
        public int week { get; set; }
        public int year { get; set; }
        public int month { get; set; }
        public int case_count { get; set; }
    }
}