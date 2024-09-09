using System;
using System.Collections.Generic;

namespace irish_housing_abm
{
    public class ConstructionCompany
    {
        private const int BaseAnnualNewHouseCount = 20000;
        private readonly double scaleFactor;
        public const double MinimumAreaPerPerson = 20.0;
        public event EventHandler<HouseEventArgs> HouseConstructed;

        public ConstructionCompany(double scaleFactor)
        {
            this.scaleFactor = scaleFactor;
        }

        public void ConstructNewHouses(List<House> houses, List<Household> households)
        {
            int scaledHouseCount = (int)(BaseAnnualNewHouseCount / scaleFactor);
            for (int i = 0; i < scaledHouseCount; i++)
            {
                double size = DrawSizeFromDistribution();
                int quality = DrawQualityFromDistribution();
                HouseType type = DetermineHouseType();
                double initialPrice = CalculateInitialPrice(size, quality, type);

                var newHouse = new House(size, quality, type, initialPrice);
                if (HouseIsSuitableForMarket(newHouse, households))
                {
                    houses.Add(newHouse);
                    OnHouseConstructed(newHouse);
                }
            }
        }

        private bool HouseIsSuitableForMarket(House house, List<Household> households)
        {
            return households.Any(household => house.Size >= MinimumAreaPerPerson * household.Size);
        }

        private double DrawSizeFromDistribution()
        {
            // Normal distribution with mean 150 and standard deviation 50
            double mean = 150;
            double stdDev = 50;
            return Math.Max(50, Math.Min(300, mean + stdDev * RandomNumberGenerator.NextGaussian()));
        }

        private int DrawQualityFromDistribution()
        {
            // Uniform distribution of quality scores from 1 to 10
            return RandomNumberGenerator.Next(1, 11);
        }

        private HouseType DetermineHouseType()
        {
            double random = RandomNumberGenerator.NextDouble();
            if (random < 0.6) return HouseType.Buy;
            else if (random < 0.9) return HouseType.PrivateRent;
            else return HouseType.SocialRent;
        }

        private double CalculateInitialPrice(double size, int quality, HouseType type)
        {
            
            double basePrice = size * 2000; 
            double qualityMultiplier = 0.5 + (quality * 0.1); 
            double typeMultiplier = type == HouseType.Buy ? 1.2 : 1.0; 

            return basePrice * qualityMultiplier * typeMultiplier;
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