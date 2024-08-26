using System;
using System.Collections.Generic;

namespace irish_housing_abm
{
    public class Bank
    {
        public List<Household> MortgagedHouseholds { get; private set; }
        public double InterestRate { get; private set; }
        private const double MaxLoanToValueRatio = 0.9; // 90% LTV
        private const double MaxDebtToIncomeRatio = 3.0;

        public Bank(double initialInterestRate)
        {
            MortgagedHouseholds = new List<Household>();
            InterestRate = initialInterestRate;
        }

        public void UpdateInterestRate(double newRate)
        {
            if (newRate < 0)
                throw new ArgumentException("Interest rate cannot be negative");
            InterestRate = newRate;
        }

        public bool ApproveMortgage(Household household, double housePrice, double downPayment, int loanTermYears)
        {
            double loanAmount = housePrice - downPayment;
            double loanToValueRatio = loanAmount / housePrice;
            double monthlyPayment = CalculateMonthlyPayment(loanAmount, loanTermYears, InterestRate);

            if (loanToValueRatio > MaxLoanToValueRatio)
                return false;

            if (household.TotalIncome < MaxDebtToIncomeRatio * monthlyPayment)
                return false;

            if (household.Wealth < downPayment)
                return false;

            household.UpdateWealth(-downPayment);
            household.LoanAmount = loanAmount;
            household.LoanTermYears = loanTermYears;
            MortgagedHouseholds.Add(household);
            return true;
        }

        public void CollectPayments()
        {
            List<Household> defaultedHouseholds = new List<Household>();

            foreach (var household in MortgagedHouseholds)
            {
                double monthlyPayment = CalculateMonthlyPayment(household.LoanAmount, household.LoanTermYears, InterestRate);
                double interestPayment = household.LoanAmount * (InterestRate / 12 / 100);
                double principalPayment = monthlyPayment - interestPayment;

                if (household.Wealth >= monthlyPayment)
                {
                    household.UpdateWealth(-monthlyPayment);
                    household.LoanAmount -= principalPayment;

                    if (household.LoanAmount <= 0)
                    {
                        household.LoanAmount = 0;
                        household.LoanTermYears = 0;
                        defaultedHouseholds.Add(household); // Loan fully paid
                    }
                }
                else
                {
                    Console.WriteLine($"Household (Income: {household.TotalIncome}) has defaulted on the mortgage.");
                    defaultedHouseholds.Add(household);
                    // Implement foreclosure logic here
                }
            }

            foreach (var defaultedHousehold in defaultedHouseholds)
            {
                MortgagedHouseholds.Remove(defaultedHousehold);
            }
        }

        public double CalculateMonthlyPayment(double loanAmount, int loanTermYears, double interestRate)
        {
            int n = loanTermYears * 12;
            double r = interestRate / 12 / 100;
            return (loanAmount * r * Math.Pow(1 + r, n)) / (Math.Pow(1 + r, n) - 1);
        }
    }
}