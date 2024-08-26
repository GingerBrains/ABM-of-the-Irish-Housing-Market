using System.Collections.Generic;

namespace irish_housing_abm
{
    public class HouseholdDataStore
    {
        public Dictionary<string, int> HouseholdTypeCounts { get; set; }
        public Dictionary<string, int> ChildAgeGroupCounts { get; set; }
        public Dictionary<string, int> AdultAgeGroupCounts { get; set; }
        public Dictionary<ContractType, int> HousingTypeCounts { get; set; }
        public Dictionary<int, int> ChildrenDistribution {  get; set; }

        public HouseholdDataStore()
        {
            HouseholdTypeCounts = new Dictionary<string, int>
            {
                { "OnePerson", 425974 },
                { "MarriedCouple", 274417 + 79912 },
                { "MarriedCoupleWithChildren", 541578 + 78409 },
                { "OneMotherWithChildren", 155583 },
                { "OneFatherWithChildren", 26812 }
            };

            ChildrenDistribution = new Dictionary<int, int>
            {
                { 1, 346938 },
                { 2, 323796 },
                { 3, 157160 },
                { 4, 44846 },
                { 5, 9593 },
                { 6, 2392 },
                { 7, 1174 } // Including 7 or more children as one category
            };

            ChildAgeGroupCounts = new Dictionary<string, int>
            {
                { "PreSchool", 331515 },
                { "Primary", 548693 },
                { "Secondary", 371588 }
            };



        AdultAgeGroupCounts = new Dictionary<string, int>
            {
                { "19-24", 331208 },
                { "25-64", 2541294 },
                { "Over65", 637567 }
            };

            HousingTypeCounts = new Dictionary<ContractType, int>
            {
                { ContractType.OwnerOccupiedWithoutLoan, 679718 },
                { ContractType.OwnerOccupiedWithLoan, 531207 },
                { ContractType.RentedFromLandlord, 348493 },
                { ContractType.SocialHousing, 165178 }
            };


        }
    }
}
