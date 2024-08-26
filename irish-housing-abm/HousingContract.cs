namespace irish_housing_abm
{
    public class HousingContract
    {
        public ContractType Type { get; set; }
        public int RunTimeMonths { get; private set; }
        public double RemainingMortgage { get; private set; }
        public double MonthlyCost { get; private set; }

        public HousingContract(ContractType type, double initialMortgage = 0, double monthlyCost = 0)
        {
            Type = type;
            RunTimeMonths = 0;
            RemainingMortgage = initialMortgage;
            MonthlyCost = monthlyCost;
        }

        public void UpdateContract(double monthlyMortgagePayment)
        {
            RunTimeMonths++;
            if (Type == ContractType.OwnerOccupiedWithLoan)
            {
                RemainingMortgage -= monthlyMortgagePayment;
                if (RemainingMortgage <= 0)
                {
                    RemainingMortgage = 0;
                    MonthlyCost = 0;
                    Type = ContractType.OwnerOccupiedWithoutLoan;
                }
            }
        }
    }

    public enum ContractType
    {
        OwnerOccupiedWithoutLoan,
        OwnerOccupiedWithLoan,
        RentedFromLandlord,
        SocialHousing
    }
}