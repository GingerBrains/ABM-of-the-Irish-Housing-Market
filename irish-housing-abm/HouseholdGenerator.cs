using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

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

            try
            {
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

                        try
                        {
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
                                default:
                                    throw new ArgumentException($"Unknown household type: {householdType.Key}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error creating household of type {householdType.Key}: {ex.Message}");
                            continue;
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
                    try
                    {
                        AssignHousingContract(household, totalHouseholds, dataStore);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error assigning housing contract to household {household.Id}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating households: {ex.Message}");
            }

            return households;
        }

        private static void CalculateIncomePercentiles(List<Household> households)
        {
            if (households == null || !households.Any())
            {
                throw new ArgumentException("Households list is null or empty");
            }

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
            if (household == null)
            {
                throw new ArgumentNullException(nameof(household));
            }

            double randomValue = RandomNumberGenerator.NextDouble();
            double cumulativeProbability = 0;
            foreach (var housingType in dataStore.HousingTypeCounts)
            {
                cumulativeProbability += (double)housingType.Value / totalHouseholds;
                if (randomValue <= cumulativeProbability)
                {
                    household.Contract = new HousingContract(
                        type: housingType.Key,
                        house: null,
                        initialMortgage: 0,
                        monthlyCost: 0
                    );

                    return;
                }
            }
            throw new InvalidOperationException("Failed to assign a housing contract");
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
                        _ => throw new ArgumentException($"Invalid age group: {ageGroup.Key}")
                    };
                }
            }
            throw new InvalidOperationException("Failed to generate a random age");
        }

        private static int GenerateCorrelatedAge(int firstAge, Dictionary<string, int> ageGroupCounts, int minAgeDifference = -10, int maxAgeDifference = 10)
        {
            int minAge = Math.Max(18, firstAge + minAgeDifference);
            int maxAge = Math.Min(80, firstAge + maxAgeDifference);

            var eligibleAgeGroups = ageGroupCounts.Where(ag =>
                (ag.Key == "19-24" && minAge <= 24) ||
                (ag.Key == "25-64" && maxAge >= 25 && minAge <= 64) ||
                (ag.Key == "Over65" && maxAge >= 65))
                .ToDictionary(ag => ag.Key, ag => ag.Value);

            int secondAge = GenerateRandomAge(eligibleAgeGroups);

            return Math.Max(minAge, Math.Min(maxAge, secondAge));
        }

        private static Household CreateOnePersonHousehold(HouseholdDataStore dataStore)
        {
            int age = GenerateRandomAge(dataStore.AdultAgeGroupCounts);
            string gender = RandomNumberGenerator.NextDouble() < 0.5 ? "male" : "female";
            double income = IncomeCalculator.GetIncome(age, gender);
            var members = new List<Person> { new Person(age, gender, income) };
            return new Household(GetNextId(), members, 0, null);
        }

        private static Household CreateMarriedCoupleHousehold(HouseholdDataStore dataStore)
        {
            int age1 = GenerateRandomAge(dataStore.AdultAgeGroupCounts);
            int age2 = GenerateCorrelatedAge(age1, dataStore.AdultAgeGroupCounts);
            double income1 = IncomeCalculator.GetIncome(age1, "male");
            double income2 = IncomeCalculator.GetIncome(age2, "female");
            var members = new List<Person>
            {
                new Person(age1, "male", income1),
                new Person(age2, "female", income2)
            };
            return new Household(GetNextId(), members, 0, null);
        }

        private static Household CreateMarriedCoupleWithChildrenHousehold(HouseholdDataStore dataStore)
        {
            int adultAge1 = GenerateRandomAge(dataStore.AdultAgeGroupCounts);
            int adultAge2 = GenerateCorrelatedAge(adultAge1, dataStore.AdultAgeGroupCounts);
            int numberOfChildren = GenerateNumberOfChildren(dataStore.ChildrenDistribution);
            double income1 = IncomeCalculator.GetIncome(adultAge1, "male");
            double income2 = IncomeCalculator.GetIncome(adultAge2, "female");
            var members = new List<Person>
            {
                new Person(adultAge1, "male", income1),
                new Person(adultAge2, "female", income2)
            };

            int youngestParentAge = Math.Min(adultAge1, adultAge2);
            for (int i = 0; i < numberOfChildren; i++)
            {
                int maxChildAge = youngestParentAge - 18; // Assuming minimum parent age is 18
                int childAge = RandomNumberGenerator.Next(0, maxChildAge + 1);
                members.Add(new Person(childAge, RandomNumberGenerator.NextDouble() < 0.5 ? "male" : "female", 0));
            }
            return new Household(GetNextId(), members, 0, null);
        }

        private static Household CreateSingleParentHousehold(HouseholdDataStore dataStore, string parentGender)
        {
            int adultAge = GenerateRandomAge(dataStore.AdultAgeGroupCounts);
            while (adultAge < 18)
            {
                adultAge = GenerateRandomAge(dataStore.AdultAgeGroupCounts);
            }
            int numberOfChildren = GenerateNumberOfChildren(dataStore.ChildrenDistribution);
            double income = IncomeCalculator.GetIncome(adultAge, parentGender);
            var members = new List<Person> { new Person(adultAge, parentGender, income) };

            for (int i = 0; i < numberOfChildren; i++)
            {
                int maxChildAge = adultAge - 18; // Assuming minimum parent age is 18
                int childAge = RandomNumberGenerator.Next(0, maxChildAge + 1);
                members.Add(new Person(childAge, RandomNumberGenerator.NextDouble() < 0.5 ? "male" : "female", 0));
            }
            return new Household(GetNextId(), members, 0, null);
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
    }
}