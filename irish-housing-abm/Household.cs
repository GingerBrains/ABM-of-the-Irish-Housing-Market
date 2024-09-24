using System;
using System.Collections.Generic;
using System.Linq;

namespace irish_housing_abm
{
    public class Household
    {
        public int Id { get; private set; }
        public List<Person> Members { get; private set; }
        public int WaitlistTime { get; set; }
        public HousingContract Contract { get; set; }
        public bool WantToMove { get; set; }
        public double LoanAmount { get; set; }
        public int LoanTermYears { get; set; }
        public int? IncomePercentile { get; set; }

        public Household(int id, List<Person> members, int incomePercentile, HousingContract contract)
        {
            Id = id;
            Members = members;
            IncomePercentile = incomePercentile;
            Contract = contract;
            WaitlistTime = 0;
            WantToMove = false;
            LoanAmount = 0;
            LoanTermYears = 0;
        }

        public int Size => Members.Count;
        public double TotalIncome => Members.Sum(m => m.Income);

        public void UpdateMemberAges()
        {
            foreach (var member in Members)
            {
                member.IncrementAge();
            }
        }

        public void UpdateMemberIncomes()
        {
            foreach (var member in Members)
            {
                member.UpdateIncome(IncomeCalculator.GetIncome(member.Age, member.Gender));
            }
        }

        public void IncrementWaitlistTime()
        {
            WaitlistTime++;
        }

        public void UpdateContract(double monthlyMortgagePayment)
        {
            if (Contract != null)
            {
                Contract.UpdateContract(monthlyMortgagePayment);
                if (Contract.Type == ContractType.OwnerOccupiedWithoutLoan)
                {
                    LoanAmount = 0;
                    LoanTermYears = 0;
                }
            }
        }

        public void DecideToMove(List<House> suitableHouses, double baseMovementCost, Func<Household, House, bool> canAffordHouseFunc, Func<double, int, double, double> calculateMonthlyPaymentFunc)
        {
            House currentHouse = this.Contract?.House;
            double currentUtility = CalculateUtility(currentHouse, 0);

            double bestUtility = currentUtility;
            House bestHouse = null;

            foreach (var house in suitableHouses)
            {
                if (canAffordHouseFunc(this, house))
                {
                    double movementCost = CalculateMovementCost(baseMovementCost, currentHouse, house);
                    double utility = CalculateUtility(house, movementCost);

                    if (utility > bestUtility)
                    {
                        bestUtility = utility;
                        bestHouse = house;
                    }
                }
            }

            // Calculate the percentage improvement in utility
            double percentageImprovement = (bestUtility - currentUtility) / currentUtility;

            // Calculate a probability of moving based on the percentage improvement
            double moveProbability = Math.Min(1, percentageImprovement * 3); // Adjust the multiplier as needed

            // Decide to move based on the calculated probability
            WantToMove = RandomNumberGenerator.NextDouble() < moveProbability;
        }

        private double CalculateUtility(House house, double movementCost)
        {
            if (house == null)
            {
                return -10 + RandomNumberGenerator.NextGaussian(0, 2);
            }

            double spacePerPerson = house.Size / this.Size;
            double incomeRatio = house.Price / (TotalIncome * 5); // 5 years of income

            double deterministic = 0.2 * spacePerPerson * house.Quality - movementCost - incomeRatio;
            double stochastic = RandomNumberGenerator.NextGaussian(0, 2);

            return deterministic + stochastic;
        }

        private double CalculateMovementCost(double baseMovementCost, House currentHouse, House newHouse)
        {
            double cost = baseMovementCost;

            if (currentHouse != null && Contract != null)
            {
                cost *= Math.Max(0, 1 - (Contract.RunTimeMonths / 120.0));
            }

            if (currentHouse != null && currentHouse.Type != newHouse.Type)
            {
                cost *= 1.5;
            }

            return cost;
        }

        public bool IsInterestedInSocialHousing()
        {
            return Contract == null && TotalIncome < 30000;
        }

        public void ResetWaitlistTime()
        {
            WaitlistTime = 0;
        }
    }
}