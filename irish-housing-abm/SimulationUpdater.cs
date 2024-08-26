using System;
using System.Collections.Generic;
using System.Linq;

namespace irish_housing_abm
{
    public class SimulationUpdater
    {
        //private Random random;
        private Bank bank;
        private ConstructionCompany constructionCompany;

        public SimulationUpdater(Bank bank, ConstructionCompany constructionCompany)
        {
            //this.random = new Random();
            this.bank = bank;
            this.constructionCompany = constructionCompany;
        }

        public void PerformMonthlyUpdates(List<Household> households, List<House> houses)
        {

            List<House> availableHouses = houses.Where(h => h.IsAvailable()).ToList();
            double baseMovementCost = 2.0; // Adjust this value as needed

            foreach (var household in households)
            {
                household.DecideToMove(availableHouses, baseMovementCost);
            }
            UpdateHousePrices(houses);
            UpdateContracts(households);
            UpdateHouseholdWealth(households);
            FindBestOptions(households);
            ActOnBestOptions(households, houses);
            AssignSocialHousing(households, houses);
            ProcessNewListings(houses);
            bank.CollectPayments();
        }

        public void PerformYearlyUpdates(List<Household> households, List<House> houses, ref double taxRate)
        {
            UpdateIncomes(households);
            SetTaxRate(households, ref taxRate);
            ConstructNewHouses(houses, households);
        }

        private void UpdateHousePrices(List<House> houses)
        {
            foreach (var house in houses)
            {
                double priceChange = (RandomNumberGenerator.NextDouble() - 0.5) * 0.1; // -5% to +5%
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
                        // Calculate monthly mortgage payment
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

        private void UpdateHouseholdWealth(List<Household> households)
        {
            foreach (var household in households)
            {
                double monthlyIncome = household.TotalIncome / 12;
                double monthlyExpenses = monthlyIncome * 0.6; // Assume 60% of income goes to expenses
                household.UpdateWealth(monthlyIncome - monthlyExpenses);
            }
        }

        private void FindBestOptions(List<Household> households)
        {
            foreach (var household in households)
            {
                if (RandomNumberGenerator.NextDouble() < 0.05) // 5% chance of wanting to move
                {
                    household.WantToMove = true;
                }

                // Check if the household is interested in social housing
                if (household.IsInterestedInSocialHousing())
                {
                    household.IncrementWaitlistTime();
                }
                else
                {
                    household.ResetWaitlistTime();
                }
            }
        }

        private void ActOnBestOptions(List<Household> households, List<House> houses)
        {
            foreach (var household in households.Where(h => h.WantToMove))
            {
                House bestHouse = houses.Where(h => h.IsAvailable())
                                        .OrderBy(h => Math.Abs(h.Size - household.Size * 20))
                                        .FirstOrDefault();

                if (bestHouse != null && household.Wealth >= bestHouse.Price * 0.2) // 20% down payment
                {
                    if (bank.ApproveMortgage(household, bestHouse.Price, bestHouse.Price * 0.2, 30))
                    {
                        bestHouse.ChangeOwnership(household);
                        household.Contract = new HousingContract(ContractType.OwnerOccupiedWithLoan);
                        household.WantToMove = false;
                    }
                }
            }
        }

        private void AssignSocialHousing(List<Household> households, List<House> houses)
        {
            // Get all available social houses
            var availableSocialHouses = houses.Where(h => h.Type == HouseType.SocialRent && h.IsAvailable()).ToList();

            // Get all households interested in social housing
            var interestedHouseholds = households.Where(h => h.IsInterestedInSocialHousing()).ToList();

            foreach (var house in availableSocialHouses)
            {
                if (interestedHouseholds.Any())
                {
                    // Find the household with the longest waiting time
                    var selectedHousehold = interestedHouseholds
                        .OrderByDescending(h => h.WaitlistTime)
                        .First();

                    // Assign the house to the selected household
                    house.ChangeOwnership(selectedHousehold);
                    selectedHousehold.Contract = new HousingContract(ContractType.SocialHousing);
                    selectedHousehold.WaitlistTime = 0; // Reset waiting time

                    // Remove the selected household from the interested list
                    interestedHouseholds.Remove(selectedHousehold);
                }
            }

            // Increment waiting time for remaining interested households
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
            double averageWealth = households.Average(h => h.Wealth);
            if (averageWealth > 500000)
                taxRate += 0.01;
            else if (averageWealth < 200000)
                taxRate -= 0.01;
            taxRate = Math.Clamp(taxRate, 0.1, 0.4); // Keep tax rate between 10% and 40%
        }

        private void ConstructNewHouses(List<House> houses, List<Household> households)
        {
            constructionCompany.ConstructNewHouses(houses, households);
        }
    }
}