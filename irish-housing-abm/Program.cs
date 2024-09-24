using System;
using System.IO;

namespace irish_housing_abm
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting Irish Housing ABM Simulation");

            // Configuration
            int years = 12; // Run for 10 years
            double scaleFactor = 100; // 1 simulated household represents 100 real households

            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string outputPath = Path.Combine(desktopPath, "SimulationResults.xlsx");

            try
            {
                using (var simulation = new Simulation())
                {
                    simulation.Run(years, scaleFactor);
                }

                Console.WriteLine($"Simulation complete. Results saved to: {outputPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred during the simulation: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }

            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }
    }
}