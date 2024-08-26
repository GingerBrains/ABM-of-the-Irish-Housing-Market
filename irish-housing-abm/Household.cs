namespace irish_housing_abm
{
    public class Household
    {
        public int Id { get; private set; }
        public List<Person> Members { get; private set; }
        public double Wealth { get; set; }
        public int WaitlistTime { get; set; }
        public HousingContract Contract { get; set; }
        public bool WantToMove { get; set; }
        public double LoanAmount { get; set; }
        public int LoanTermYears { get; set; }
        public int? IncomePercentile { get; set; }


        public Household(int id, List<Person> members, int incomePercentile, double wealth, HousingContract contract)
        {
            Id = id;
            Members = members;
            IncomePercentile = incomePercentile;
            Wealth = wealth;
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

        public void UpdateWealth(double amount)
        {
            Wealth += amount;
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

        public void DecideToMove()
        {
            // Implement logic 

        }

        public bool IsInterestedInSocialHousing()
        {
            // This is a simple implementation. 
            return Contract == null && TotalIncome < 30000; // income threshold
        }


        public void ResetWaitlistTime()
        {
            WaitlistTime = 0;
        }
    }

    


}
