namespace irish_housing_abm
{
    public class House
    {
        public double Size { get; private set; } // square meters
        public int Quality { get; private set; } // from 0 to 10
        public HouseType Type { get; private set; }
        public Household Owner { get; private set; }
        public double Price { get; private set; }

        public House(double size, int quality, HouseType type, double initialPrice, Household owner = null)
        {
            Size = size;
            Quality = quality;
            Type = type;
            Price = initialPrice;
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

        public void UpdatePrice(double factor)
        {
            Price *= factor;
        }

        public bool IsAvailable()
        {
            return Owner == null;
        }
    }

    public enum HouseType
    {
        SocialRent,
        PrivateRent,
        Buy
    }
}