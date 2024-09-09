using System;
using System.Collections.Generic;
using System.Linq;
using irish_housing_abm;

public class PopulationUpdater
{
    private PopulationModel populationModel;
    private Random random;

    public PopulationUpdater(PopulationModel populationModel)
    {
        this.populationModel = populationModel;
        this.random = new Random();
    }

    public void UpdatePopulation(List<Household> households)
    {
        List<Household> householdsToRemove = new List<Household>();
        List<Person> newborns = new List<Person>();

        foreach (var household in households)
        {
            List<Person> membersToRemove = new List<Person>();

            foreach (var person in household.Members)
            {
                // Age the person
                person.IncrementAge();

                // Apply mortality rate
                double mortalityRate = populationModel.GetMortalityRate(person.Gender == "female", person.Age);
                if (random.NextDouble() < mortalityRate)
                {
                    membersToRemove.Add(person);
                }

                // Handle births for women of childbearing age
                if (person.Gender == "female" && person.Age >= 15 && person.Age <= 49)
                {
                    double birthRate = populationModel.GetBirthRate(person.Age - 15);
                    if (random.NextDouble() < birthRate)
                    {
                        newborns.Add(new Person(0, random.NextDouble() < 0.5 ? "female" : "male", 0));
                    }
                }
            }

            // Remove deceased members
            foreach (var member in membersToRemove)
            {
                household.Members.Remove(member);
            }

            // Add newborns to the household
            household.Members.AddRange(newborns);
            newborns.Clear();

            // Remove empty households
            if (household.Members.Count == 0)
            {
                householdsToRemove.Add(household);
            }
        }

        // Remove empty households
        foreach (var household in householdsToRemove)
        {
            households.Remove(household);
        }

        // Handle migration
        int netMigration = (int)(populationModel.NetMigrationPerYear / populationModel.InitialPopulationSize * households.Count);
        if (netMigration > 0)
        {
            for (int i = 0; i < netMigration; i++)
            {
                households.Add(CreateMigrantHousehold(households.Count));
            }
        }
        else
        {
            for (int i = 0; i < Math.Abs(netMigration); i++)
            {
                if (households.Count > 0)
                {
                    households.RemoveAt(random.Next(households.Count));
                }
            }
        }
    }

    private Household CreateMigrantHousehold(int id)
    {
        bool isFemale = random.NextDouble() < populationModel.MigrantSexRatio;
        int ageGroup = SelectMigrantAgeGroup(isFemale);
        string gender = isFemale ? "female" : "male";
        int age = ageGroup + random.Next(0, 5);
        double income = GenerateIncome(age, gender);
        var person = new Person(age, gender, income);
        return new Household(id, new List<Person> { person }, 0, null);
    }

    private int SelectMigrantAgeGroup(bool isFemale)
    {
        double[] distribution = isFemale ? populationModel.NetMigrationAgeDistF : populationModel.NetMigrationAgeDistM;
        double randomValue = random.NextDouble();
        double cumulativeProbability = 0;
        for (int i = 0; i < distribution.Length; i++)
        {
            cumulativeProbability += distribution[i];
            if (randomValue < cumulativeProbability)
            {
                return i;
            }
        }
        return distribution.Length - 1;
    }

    private double GenerateIncome(int age, string gender)
    {
        // This is a placeholder. Implement your income generation logic here.
        double baseIncome = 30000;
        double ageFactor = Math.Min(1, age / 65.0);
        double genderFactor = gender == "female" ? 0.85 : 1; // gender pay gap
        return baseIncome * ageFactor * genderFactor * (1 + random.NextDouble() * 0.5);
    }
}