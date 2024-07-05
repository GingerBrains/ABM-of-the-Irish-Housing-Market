namespace irish_housing_abm

{
    public class Bank
    {
        public List<Household> MortgagedHouseholds { get; set; }
        public double InterestRate { get; set; } 

        public Bank(double initialInterestRate)
        {
            MortgagedHouseholds = new List<Household>();
            InterestRate = initialInterestRate;
        }

        public void UpdateInterestRate(double newRate)
        {
            InterestRate = newRate;
        }

        public bool ApproveMortgage(Household household, double housePrice, double downPayment, int loanTermYears)
        {
            double loanAmount = housePrice - downPayment;
            double monthlyPayment = CalculateMonthlyPayment(loanAmount, loanTermYears, InterestRate);

            if (household.Income >= 3 * monthlyPayment)
            {
                household.UpdateWealth(-downPayment); // Deduct down payment from wealth
                household.LoanAmount = loanAmount;
                household.LoanTermYears = loanTermYears;
                MortgagedHouseholds.Add(household);
                return true;
            }
            return false;
        }

        public void CollectPayments()
        {
            foreach (var household in MortgagedHouseholds)
            {
                double monthlyPayment = CalculateMonthlyPayment(household.LoanAmount, household.LoanTermYears, InterestRate);
                household.UpdateWealth(-monthlyPayment);

                if (household.Wealth < 0)
                {
                    // Handle default
                    Console.WriteLine($"Household (Age: {household.Age}, Income: {household.Income}) has defaulted on the mortgage.");
                    // Logic for foreclosure or other actions
                }
            }
        }

        private double CalculateMonthlyPayment(double loanAmount, int loanTermYears, double interestRate)
        {
            int n = loanTermYears * 12;
            double r = interestRate / 12 / 100;
            return (loanAmount * r) / (1 - Math.Pow(1 + r, -n));
        }
    }
}
