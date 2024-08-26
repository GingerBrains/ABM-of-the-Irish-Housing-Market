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

        public static double NextGaussian(double mean = 0.0, double standardDeviation = 1.0)
        {
            double u1 = 1.0 - NextDouble();
            double u2 = 1.0 - NextDouble();
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
            return mean + standardDeviation * randStdNormal;
        }
    }
}
