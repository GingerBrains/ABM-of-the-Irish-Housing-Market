namespace irish_housing_abm
{
    public class HousingContract
    {
        public ContractType Type { get; set; }
        public int RunTimeMonths { get;  set; }
        public double RemainingMortgage { get;  set; }
        public double MonthlyCost { get;  set; }
        public House House { get; set; }

        public HousingContract(ContractType type, House house = null, double initialMortgage = 0, double monthlyCost = 0)
        {
            Type = type;
            House = house;
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

        public void ResetRuntime()
        {
            RunTimeMonths = 0;
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