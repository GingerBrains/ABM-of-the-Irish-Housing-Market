using System;
using System.Collections.Generic;
using System.Linq;

namespace irish_housing_abm
{
    public static class HouseholdGenerator
    {
        private static int currentId = 0;

        private static int GetNextId()
        {
            return Interlocked.Increment(ref currentId);
        }

        public static List<Household> GenerateHouseholds(double scaleFactor, HouseholdDataStore dataStore)
        {
            List<Household> households = new List<Household>();
            int totalHouseholds = 0;

            foreach (var count in dataStore.HouseholdTypeCounts.Values)
            {
                totalHouseholds += count;
            }

            foreach (var householdType in dataStore.HouseholdTypeCounts)
            {
                int scaledCount = (int)Math.Round(householdType.Value / scaleFactor);

                for (int i = 0; i < scaledCount; i++)
                {
                    Household household = null;

                    switch (householdType.Key)
                    {
                        case "OnePerson":
                            household = CreateOnePersonHousehold(dataStore);
                            break;
                        case "MarriedCouple":
                            household = CreateMarriedCoupleHousehold(dataStore);
                            break;
                        case "MarriedCoupleWithChildren":
                            household = CreateMarriedCoupleWithChildrenHousehold(dataStore);
                            break;
                        case "OneMotherWithChildren":
                            household = CreateSingleParentHousehold(dataStore, "female");
                            break;
                        case "OneFatherWithChildren":
                            household = CreateSingleParentHousehold(dataStore, "male");
                            break;
                    }

                    if (household != null)
                    {
                        households.Add(household);
                    }
                }
            }

            
            CalculateIncomePercentiles(households);

            
            foreach (var household in households)
            {
                AssignHousingContract(household, totalHouseholds, dataStore);
            }

            return households;
        }

        private static Household CreateOnePersonHousehold(HouseholdDataStore dataStore)
        {
            int age = GenerateRandomAge(dataStore.AdultAgeGroupCounts);
            string gender = RandomNumberGenerator.NextDouble() < 0.5 ? "male" : "female";
            double income = IncomeCalculator.GetIncome(age, gender);
            double wealth = income * 10;  // Example logic for wealth
            var members = new List<Person> { new Person(age, gender, income) };
            return new Household(GetNextId(), members, 0, wealth, null);
        }

        private static Household CreateMarriedCoupleHousehold(HouseholdDataStore dataStore)
        {
            int age1 = GenerateRandomAge(dataStore.AdultAgeGroupCounts);
            int age2 = GenerateRandomAge(dataStore.AdultAgeGroupCounts);
            double income1 = IncomeCalculator.GetIncome(age1, "male");
            double income2 = IncomeCalculator.GetIncome(age2, "female");
            double totalIncome = income1 + income2;
            double wealth = totalIncome * 10;  // Example logic for wealth
            var members = new List<Person>
            {
                new Person(age1, "male", income1),
                new Person(age2, "female", income2)
            };
            return new Household(GetNextId(), members, 0, wealth, null);
        }

        private static Household CreateMarriedCoupleWithChildrenHousehold(HouseholdDataStore dataStore)
        {
            int adultAge1 = GenerateRandomAge(dataStore.AdultAgeGroupCounts);
            int adultAge2 = GenerateRandomAge(dataStore.AdultAgeGroupCounts);
            int numberOfChildren = GenerateNumberOfChildren(dataStore.ChildrenDistribution);
            double income1 = IncomeCalculator.GetIncome(adultAge1, "male");
            double income2 = IncomeCalculator.GetIncome(adultAge2, "female");
            double totalIncome = income1 + income2;
            double wealth = totalIncome * 10;  // Example logic for wealth
            var members = new List<Person>
            {
                new Person(adultAge1, "male", income1),
                new Person(adultAge2, "female", income2)
            };
            for (int i = 0; i < numberOfChildren; i++)
            {
                int childAge = GenerateRandomAge(dataStore.ChildAgeGroupCounts);
                members.Add(new Person(childAge, RandomNumberGenerator.NextDouble() < 0.5 ? "male" : "female", 0));
            }
            return new Household(GetNextId(), members, 0, wealth, null);
        }

        private static Household CreateSingleParentHousehold(HouseholdDataStore dataStore, string parentGender)
        {
            int adultAge = GenerateRandomAge(dataStore.AdultAgeGroupCounts);
            int numberOfChildren = GenerateNumberOfChildren(dataStore.ChildrenDistribution);
            double income = IncomeCalculator.GetIncome(adultAge, parentGender);
            double wealth = income * 10;  // Example logic for wealth
            var members = new List<Person> { new Person(adultAge, parentGender, income) };
            for (int i = 0; i < numberOfChildren; i++)
            {
                int childAge = GenerateRandomAge(dataStore.ChildAgeGroupCounts);
                members.Add(new Person(childAge, RandomNumberGenerator.NextDouble() < 0.5 ? "male" : "female", 0));
            }
            return new Household(GetNextId(), members, 0, wealth, null);
        }

        private static int GenerateNumberOfChildren(Dictionary<int, int> childrenDistribution)
        {
            int total = childrenDistribution.Values.Sum();
            int randomValue = RandomNumberGenerator.Next(0, total);
            int cumulative = 0;
            foreach (var kvp in childrenDistribution)
            {
                cumulative += kvp.Value;
                if (randomValue < cumulative)
                {
                    return kvp.Key;
                }
            }
            throw new InvalidOperationException("Failed to generate number of children");
        }

        private static int GenerateRandomAge(Dictionary<string, int> ageGroupCounts)
        {
            int totalPopulation = ageGroupCounts.Values.Sum();
            int randomValue = RandomNumberGenerator.Next(0, totalPopulation);
            int cumulativeCount = 0;
            foreach (var ageGroup in ageGroupCounts)
            {
                cumulativeCount += ageGroup.Value;
                if (randomValue < cumulativeCount)
                {
                    return ageGroup.Key switch
                    {
                        "PreSchool" => RandomNumberGenerator.Next(0, 5),
                        "Primary" => RandomNumberGenerator.Next(6, 12),
                        "Secondary" => RandomNumberGenerator.Next(13, 18),
                        "19-24" => RandomNumberGenerator.Next(19, 25),
                        "25-64" => RandomNumberGenerator.Next(25, 65),
                        "Over65" => RandomNumberGenerator.Next(65, 81),
                        _ => throw new ArgumentException("Invalid age group")
                    };
                }
            }
            throw new InvalidOperationException("Failed to generate a random age");
        }

        private static void CalculateIncomePercentiles(List<Household> households)
        {
            var sortedIncomes = households.Select(h => h.TotalIncome).OrderBy(i => i).ToList();
            int totalHouseholds = households.Count;
            foreach (var household in households)
            {
                int index = sortedIncomes.BinarySearch(household.TotalIncome);
                if (index < 0) index = ~index;
                household.IncomePercentile = (int)Math.Round((double)index / totalHouseholds * 100);
            }
        }

        private static void AssignHousingContract(Household household, int totalHouseholds, HouseholdDataStore dataStore)
        {
            double randomValue = RandomNumberGenerator.NextDouble();
            double cumulativeProbability = 0;
            foreach (var housingType in dataStore.HousingTypeCounts)
            {
                cumulativeProbability += (double)housingType.Value / totalHouseholds;
                if (randomValue <= cumulativeProbability)
                {
                    household.Contract = new HousingContract(housingType.Key);
                    return;
                }
            }
            throw new InvalidOperationException("Failed to assign a housing contract");
        }
    }
}