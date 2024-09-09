using System;
using System.Collections.Generic;
using System.Linq;

namespace irish_housing_abm
{
    public class SimulationUpdater
    {
        private Bank bank;
        private ConstructionCompany constructionCompany;
        private PopulationModel populationModel;

        public SimulationUpdater(Bank bank, ConstructionCompany constructionCompany, PopulationModel populationModel)
        {
            this.bank = bank;
            this.constructionCompany = constructionCompany;
            this.populationModel = populationModel;
        }

        public void PerformMonthlyUpdates(List<Household> households, List<House> houses)
        {
            Console.WriteLine("    Updating house prices");
            UpdateHousePrices(houses);
            Console.WriteLine("    Updating contracts");
            UpdateContracts(households);
            Console.WriteLine("    Deciding to move");
            DecideToMove(households, houses);
            Console.WriteLine("    Acting on best options");
            ActOnBestOptions(households, houses);
            Console.WriteLine("    Assigning social housing");
            AssignSocialHousing(households, houses);
            Console.WriteLine("    Processing new listings");
            ProcessNewListings(houses);
            Console.WriteLine("    Collecting bank payments");
            bank.CollectPayments();
        }

        public void PerformYearlyUpdates(List<Household> households, List<House> houses, ref double taxRate)
        {
            Console.WriteLine("    Updating population");
            UpdatePopulation(households);
            Console.WriteLine("    Updating incomes");
            UpdateIncomes(households);
            Console.WriteLine("    Setting tax rate");
            SetTaxRate(households, ref taxRate);
            Console.WriteLine("    Constructing new houses");
            ConstructNewHouses(houses, households);
        }

        private void UpdateHousePrices(List<House> houses)
        {
            foreach (var house in houses)
            {
                double priceChange = (RandomNumberGenerator.NextDouble() - 0.5) * 0.1;
                house.UpdatePrice(1 + priceChange);
            }
        }

        private void UpdateContracts(List<Household> households)
        {
            foreach (var household in households)
            {
                if (household.Contract != null)
                {
                    double monthlyMortgagePayment = 0;
                    if (household.Contract.Type == ContractType.OwnerOccupiedWithLoan)
                    {
                        monthlyMortgagePayment = bank.CalculateMonthlyPayment(
                            household.LoanAmount,
                            household.LoanTermYears,
                            bank.InterestRate
                        );
                    }

                    household.UpdateContract(monthlyMortgagePayment);
                }
            }
        }

        private void DecideToMove(List<Household> households, List<House> houses)
        {
            Console.WriteLine("    Starting DecideToMove");
            List<House> availableHouses = houses.Where(h => h.IsAvailable()).ToList();
            Console.WriteLine($"    Available Houses: {availableHouses.Count}");

            double baseMovementCost = 2.0;

            int householdsConsidered = 0;
            int householdsWantingToMove = 0;

            foreach (var household in households)
            {
                household.DecideToMove(availableHouses, baseMovementCost, CanAffordHouse, bank.CalculateMonthlyPayment);

                householdsConsidered++;
                if (household.WantToMove) householdsWantingToMove++;

                if (householdsConsidered % 1000 == 0 || householdsConsidered == households.Count)
                {
                    Console.WriteLine($"        Processed {householdsConsidered}/{households.Count} households. {householdsWantingToMove} want to move.");
                }
            }
            Console.WriteLine("    Completed DecideToMove");
        }

        private void ActOnBestOptions(List<Household> households, List<House> houses)
        {
            foreach (var household in households.Where(h => h.WantToMove))
            {
                House bestHouse = houses.Where(h => h.IsAvailable())
                                        .OrderBy(h => Math.Abs(h.Size - household.Size * 20))
                                        .FirstOrDefault();

                if (bestHouse != null && CanAffordHouse(household, bestHouse))
                {
                    if (bank.ApproveMortgage(household, bestHouse.Price, bestHouse.Price * 0.2, 30))
                    {
                        bestHouse.ChangeOwnership(household);
                        household.Contract = new HousingContract(ContractType.OwnerOccupiedWithLoan, bestHouse, bestHouse.Price * 0.8);
                        household.WantToMove = false;
                    }
                }
            }
        }

        private bool CanAffordHouse(Household household, House house)
        {
            double monthlyPayment = bank.CalculateMonthlyPayment(house.Price, 30, bank.InterestRate);
            return monthlyPayment <= household.TotalIncome * 0.3 / 12; // 30% of monthly income
        }

        private void AssignSocialHousing(List<Household> households, List<House> houses)
        {
            var availableSocialHouses = houses.Where(h => h.Type == HouseType.SocialRent && h.IsAvailable()).ToList();
            var interestedHouseholds = households.Where(h => h.IsInterestedInSocialHousing()).ToList();

            foreach (var house in availableSocialHouses)
            {
                if (interestedHouseholds.Any())
                {
                    var selectedHousehold = interestedHouseholds
                        .OrderByDescending(h => h.WaitlistTime)
                        .First();

                    house.ChangeOwnership(selectedHousehold);
                    selectedHousehold.Contract = new HousingContract(ContractType.SocialHousing, house);
                    selectedHousehold.ResetWaitlistTime();

                    interestedHouseholds.Remove(selectedHousehold);
                }
            }

            foreach (var household in interestedHouseholds)
            {
                household.IncrementWaitlistTime();
            }
        }

        private void ProcessNewListings(List<House> houses)
        {
            foreach (var house in houses.Where(h => !h.IsAvailable()))
            {
                if (RandomNumberGenerator.NextDouble() < 0.01)
                {
                    house.ChangeOwnership(null);
                }
            }
        }

        private void UpdateIncomes(List<Household> households)
        {
            foreach (var household in households)
            {
                household.UpdateMemberIncomes();
            }
        }

        private void SetTaxRate(List<Household> households, ref double taxRate)
        {
            double averageIncome = households.Average(h => h.TotalIncome);
            if (averageIncome > 50000)
                taxRate += 0.01;
            else if (averageIncome < 30000)
                taxRate -= 0.01;
            taxRate = Math.Clamp(taxRate, 0.1, 0.4);
        }

        private void ConstructNewHouses(List<House> houses, List<Household> households)
        {
            constructionCompany.ConstructNewHouses(houses, households);
        }

        private void UpdatePopulation(List<Household> households)
        {
            ApplyMortality(households);
            ApplyBirths(households);
            ApplyMigration(households);
            AgePopulation(households);
        }

        private void ApplyMortality(List<Household> households)
        {
            var membersToRemove = new List<(Household household, Person member)>();
            var householdsToRemove = new List<Household>();

            foreach (var household in households)
            {
                foreach (var person in household.Members)
                {
                    double mortalityRate = populationModel.GetMortalityRate(person.Gender == "female", person.Age);
                    if (RandomNumberGenerator.NextDouble() < mortalityRate)
                    {
                        membersToRemove.Add((household, person));
                    }
                }

                if (household.Members.Count == membersToRemove.Count(m => m.household == household))
                {
                    householdsToRemove.Add(household);
                }
            }

            // Remove deceased members after the iteration
            foreach (var (household, member) in membersToRemove)
            {
                household.Members.Remove(member);
            }

            // Remove empty households after the iteration
            foreach (var household in householdsToRemove)
            {
                households.Remove(household);
            }
        }

        private void ApplyBirths(List<Household> households)
        {
            var newMembers = new List<(Household household, Person newborn)>();

            foreach (var household in households)
            {
                foreach (var person in household.Members.Where(m => m.Gender == "female" && m.Age >= 15 && m.Age <= 49))
                {
                    double birthRate = populationModel.GetBirthRate(person.Age - 15);
                    birthRate *= (1 + populationModel.BirthTrend); // Apply birth trend

                    if (RandomNumberGenerator.NextDouble() < birthRate)
                    {
                        string childGender = RandomNumberGenerator.NextDouble() < 0.5 ? "female" : "male";
                        var newborn = new Person(0, childGender, 0);
                        newMembers.Add((household, newborn));
                    }
                }
            }

            // Add new members after the iteration is complete
            foreach (var (household, newborn) in newMembers)
            {
                household.Members.Add(newborn);
            }
        }

        private void ApplyMigration(List<Household> households)
        {
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
                        households.RemoveAt(RandomNumberGenerator.Next(0,households.Count));
                    }
                }
            }
        }

        private Household CreateMigrantHousehold(int id)
        {
            bool isFemale = RandomNumberGenerator.NextDouble() < populationModel.MigrantSexRatio;
            int ageGroup = SelectMigrantAgeGroup(isFemale);
            string gender = isFemale ? "female" : "male";
            int age = ageGroup + RandomNumberGenerator.Next(0, 5);
            double income = IncomeCalculator.GetIncome(age, gender);
            var person = new Person(age, gender, income);
            return new Household(id, new List<Person> { person }, 0, null);
        }

        private int SelectMigrantAgeGroup(bool isFemale)
        {
            double[] distribution = isFemale ? populationModel.NetMigrationAgeDistF : populationModel.NetMigrationAgeDistM;
            double randomValue = RandomNumberGenerator.NextDouble();
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

        private void AgePopulation(List<Household> households)
        {
            foreach (var household in households)
            {
                foreach (var person in household.Members)
                {
                    person.IncrementAge();
                }
            }
        }
    }
}