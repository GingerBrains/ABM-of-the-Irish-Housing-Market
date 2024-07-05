using System;
using System.Collections.Generic;

namespace irish_housing_abm
{
    public static class IncomeData
    {
        public static Dictionary<int, (double Male, double Female)> MeanIncome2020 = new Dictionary<int, (double Male, double Female)>()
        {
            { 0, (0, 0) },
            { 15, (401.66, 344.92) },
            { 25, (692.20, 628.25) },
            { 30, (927.24, 767.05) },
            { 40, (1106.87, 825.52) },
            { 50, (1106.31, 756.63) },
            { 60, (851.77, 584.27) }
        };
    }
}

