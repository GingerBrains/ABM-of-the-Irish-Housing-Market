namespace irish_housing_abm
{
    public class House
    {
        public double Size { get; set; } // square meters
        public int Quality { get; set; } // from 0 to 10
        public HouseType Type { get; set; } 
        public Household Owner { get; set; } 

        public House(double size, int quality, HouseType type, Household owner = null)
        {
            Size = size;
            Quality = quality;
            Type = type;
            Owner = owner;
        }

        public bool IsOwned()
        {
            return Owner != null;
        }

        public void ChangeOwnership(Household newOwner)
        {
            Owner = newOwner;
        }
    }

    public enum HouseType
    {
        SocialRent,
        PrivateRent,
        Buy
    }
}
