namespace irish_housing_abm
{
    public static class IncomeCalculator
    {
        public static double GetIncome(int age, string gender)
        {
            var ageGroup = GetAgeGroup(age);
            if (IncomeData.MeanIncome2020.ContainsKey(ageGroup))
            {
                var incomeTuple = IncomeData.MeanIncome2020[ageGroup];
                return gender.ToLower() switch
                {
                    "male" => incomeTuple.Male,
                    "female" => incomeTuple.Female,
                    _ => throw new ArgumentException($"Invalid gender: {gender}")
                };
            }
            throw new ArgumentException($"Income data not available for age {age}");
        }

        private static int GetAgeGroup(int age)
        {
            if (age >= 0 && age <= 14) return 0;
            if (age >= 15 && age <= 24) return 15;
            if (age >= 25 && age <= 29) return 25;
            if (age >= 30 && age <= 39) return 30;
            if (age >= 40 && age <= 49) return 40;
            if (age >= 50 && age <= 59) return 50;
            if (age >= 60) return 60;
            throw new ArgumentException($"Age {age} is out of range");
        }
    }
}

