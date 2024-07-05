namespace irish_housing_abm
{
    public class Household
    {
        public int Age { get; set; }
        public int Size { get; set; }
        public double Income { get; set; }
        public double Wealth { get; set; }
        public int WaitlistTime { get; set; }
        public HousingContract Contract { get; set; }
        public bool WantToMove { get; set; }

        // Mortgage details
        public double LoanAmount { get; set; }
        public int LoanTermYears { get; set; }

        public Household(int age, int size, int incomePercentile, double income, double wealth, HousingContract contract)
        {
            Age = age;
            Size = size;
            Income = income;
            Wealth = wealth;
            WaitlistTime = 0;
            Contract = contract;
            WantToMove = false;

            // Default mortgage details
            LoanAmount = 0;
            LoanTermYears = 0;
        }

        public void UpdateIncome(double newIncome)
        {
            Income = newIncome;
        }

        public void UpdateWealth(double amount)
        {
            Wealth += amount;
        }

        public void IncrementWaitlistTime()
        {
            WaitlistTime++;
        }

        public void DecideToMove()
        {
            // Implement logic to decide whether the household wants to move
            WantToMove = RandomNumberGenerator.NextDouble() > 0.5; // Example logic
        }
    }

    public class HousingContract
    {
        public ContractType Type { get; set; }

        public HousingContract(ContractType type)
        {
            Type = type;
        }
    }

    public enum ContractType
    {
        Buying,
        Rental,
        SocialRental
    }
}
