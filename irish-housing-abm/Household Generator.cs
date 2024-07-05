using System;
using System.Collections.Generic;

namespace irish_housing_abm
{
    public class HouseholdGenerator
    {
        public static List<Household> GenerateHouseholds(int numberOfHouseholds, double scaleFactor)
        {
            List<Household> households = new List<Household>();

            // Calculate the scaled number of households
            int scaledCount = (int)Math.Round(numberOfHouseholds / scaleFactor);

            for (int i = 0; i < scaledCount; i++)
            {
                // Generate a random household size between 1 and 6
                int size = RandomNumberGenerator.Next(1, 7);

                // Generate random minimum and maximum ages for the household
                int ageRangeMin = RandomNumberGenerator.Next(AgeConstants.MinAge, AgeConstants.MaxAge + 1);
                int ageRangeMax = RandomNumberGenerator.Next(ageRangeMin, AgeConstants.MaxAge + 1);

                // List to store the ages and genders of household members
                List<(int Age, string Gender)> members = new List<(int, string)>();

                for (int j = 0; j < size; j++)
                {
                    // Generate a random age within the specified age range
                    int age = RandomNumberGenerator.Next(ageRangeMin, ageRangeMax + 1);

                    // Randomly assign gender
                    string memberGender = RandomNumberGenerator.NextDouble() < 0.5 ? "male" : "female";

                    // Add the age and gender to the members list
                    members.Add((age, memberGender));
                }

                // Calculate the total income for the household by summing the incomes of all members
                double totalIncome = 0;
                foreach (var (age, memberGender) in members)
                {
                    totalIncome += IncomeCalculator.GetIncome(age, memberGender);
                }

                // Example logic for calculating wealth (could be more sophisticated)
                double wealth = totalIncome * 10;

                // Placeholder logic for determining the type of housing contract
                ContractType contractType = ContractType.Rental;
                HousingContract contract = new HousingContract(contractType);

                // Create a new household with the calculated attributes
                Household household = new Household(members[0].Age, size, 0, totalIncome, wealth, contract);
                households.Add(household);
            }

            return households;
        }
    }
}


public static class AgeConstants
{
    public const int MinAge = 0;
    public const int MaxAge = 80;
}