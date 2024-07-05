using System;

namespace irish_housing_abm
{
    public static class RandomNumberGenerator
    {
        private static readonly Random _random = new Random();

        public static int Next(int minValue, int maxValue)
        {
            return _random.Next(minValue, maxValue);
        }

        public static double NextDouble()
        {
            return _random.NextDouble();
        }
    }
}
