namespace irish_housing_abm
{
    public class ConstructionCompany
    {
        private readonly Random random = new Random();
        private const int AnnualNewHouseCount = 100; // Example: Construct 100 new houses annually

        // Event to notify when new houses are constructed
        public event EventHandler<HouseEventArgs> HouseConstructed;

        public void ConstructNewHouses(List<House> houses)
        {
            for (int i = 0; i < AnnualNewHouseCount; i++)
            {
                double size = DrawSizeFromDistribution();
                int quality = DrawQualityFromDistribution();
                HouseType type = DetermineHouseType(); 

                var newHouse = new House(size, quality, type);
                houses.Add(newHouse);

                // Raise event to notify that a new house has been constructed
                OnHouseConstructed(newHouse);
            }
        }

        private double DrawSizeFromDistribution()
        {
            // logic to draw size from a distribution (e.g., normal distribution)
            return 0;
        }

        private int DrawQualityFromDistribution()
        {
            // logic to draw quality from a distribution (e.g., uniform distribution)
            return 0;
        }

        private HouseType DetermineHouseType()
        {
            // logic to determine house type (e.g., random selection)
            Array values = Enum.GetValues(typeof(HouseType));
            return (HouseType)values.GetValue(random.Next(values.Length));
        }

        protected virtual void OnHouseConstructed(House newHouse)
        {
            HouseConstructed?.Invoke(this, new HouseEventArgs(newHouse));
        }
    }

    public class HouseEventArgs : EventArgs
    {
        public House ConstructedHouse { get; }

        public HouseEventArgs(House house)
        {
            ConstructedHouse = house;
        }
    }
}
