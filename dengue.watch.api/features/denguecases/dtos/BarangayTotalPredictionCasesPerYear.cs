using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace dengue.watch.api.features.denguecases.dtos;

public record BarangayTotalPredictionCasesPerYear(string barangayName, int predictedCasesCount);

