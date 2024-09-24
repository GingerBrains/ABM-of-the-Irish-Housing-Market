using irish_housing_abm;
using OfficeOpenXml;
using System.IO;

public class Simulation : IDisposable
{
    private string outputPath;
    private ExcelPackage excelPackage;
    private ExcelWorksheet worksheet;
    private List<Household> households;
    private List<House> houses;
    private PopulationModel populationModel;
    private HouseholdDataStore householdDataStore;
    private Bank bank;
    private ConstructionCompany constructionCompany;
    private SimulationUpdater simulationUpdater;

    public Simulation()
    {
        int householdMoves = 0;
        populationModel = new PopulationModel();
        householdDataStore = new HouseholdDataStore(); // Make sure to initialize this
        bank = new Bank(0.03); // 3% interest rate
        constructionCompany = new ConstructionCompany(100); // Scale factor of 100
        simulationUpdater = new SimulationUpdater(bank, constructionCompany, populationModel);
        houses = new List<House>();


        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        string outputPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "SimulationResults.xlsx");

        try
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }

            excelPackage = new ExcelPackage(new FileInfo(outputPath));
            worksheet = excelPackage.Workbook.Worksheets.Add("Monthly Statistics");

            InitializeExcelHeader();
        }
        catch (IOException ex)
        {
            Console.WriteLine($"An error occurred while managing the Excel file: {ex.Message}");
            // Handle the error appropriately
        }
    }

    private void InitializeExcelHeader()
    {
        // Monthly statistics headers
        string[] monthlyHeaders = new string[]
        {
            "Year", "Month", "Total Households", "Total Population", "Average Household Size",
            "Total Houses", "Vacant Houses", "Average House Price", "Average Rent",
            "Homeownership Rate", "Average Income", "Unemployment Rate", "Households Wanting to Move",
            "Households Moved", "Mean Area per Person" // Added new column
        };

        for (int i = 0; i < monthlyHeaders.Length; i++)
        {
            worksheet.Cells[1, i + 1].Value = monthlyHeaders[i];
        }

        // Yearly statistics headers
        var yearlySheet = excelPackage.Workbook.Worksheets.FirstOrDefault(ws => ws.Name == "Yearly Statistics")
                          ?? excelPackage.Workbook.Worksheets.Add("Yearly Statistics");

        string[] yearlyHeaders = new string[]
        {
            "Year", "Total Households", "Total Population", "Average Household Size",
            "Total Houses", "Vacant Houses", "Average House Price", "Average Rent",
            "Homeownership Rate", "Average Income", "Unemployment Rate", "Households Wanting to Move",
            "Households Moved", "Mean Area per Person" // Added new column
        };

        for (int i = 0; i < yearlyHeaders.Length; i++)
        {
            yearlySheet.Cells[1, i + 1].Value = yearlyHeaders[i];
        }
    }

    public void Run(int years, double scaleFactor)
    {
        Initialize(scaleFactor);
        InitializeExcelHeader();

        for (int year = 0; year < years; year++)
        {
            Console.WriteLine($"Starting year {year + 1}");

            for (int month = 0; month < 12; month++)
            {
                Console.WriteLine($"  Processing month {month + 1}");
                simulationUpdater.PerformMonthlyUpdates(households, houses);
                OutputMonthlyStatistics(year, month);
                Console.WriteLine($"  Completed month {month + 1} of year {year + 1}");
            }

            double taxRate = 0.3; // tax rate
            simulationUpdater.PerformYearlyUpdates(households, houses, ref taxRate);

            OutputYearlyStatistics(year, 11); // Output yearly statistics after December (month 11)

            Console.WriteLine($"Completed year {year + 1}");
        }

        worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
        var yearlySheet = excelPackage.Workbook.Worksheets["Yearly Statistics"];
        if (yearlySheet != null)
        {
            yearlySheet.Cells[yearlySheet.Dimension.Address].AutoFitColumns();
        }
        excelPackage.Save();
        Console.WriteLine("Simulation complete");
    }



    private void Initialize(double scaleFactor)
    {
        households = HouseholdGenerator.GenerateHouseholds(scaleFactor, householdDataStore);
        InitializeHouses(scaleFactor);
    }

    private void InitializeHouses(double scaleFactor)
    {
        int totalHouseholds = households.Count;
        double averageHouseholdSize = households.Average(h => h.Members.Count);

        // Use real-world housing data and apply the scale factor
        int realWorldHouses = 2006000; // Total housing stock in Ireland as of 2020 (approximate)
        int totalHouses = (int)(realWorldHouses / scaleFactor);

        // Ensure we have at least as many houses as households
        totalHouses = Math.Max(totalHouses, (int)(totalHouseholds * 1.1));

        Console.WriteLine($"Initializing {totalHouses} houses");

        for (int i = 0; i < totalHouses; i++)
        {
            double size = RandomNumberGenerator.NextGaussian(averageHouseholdSize * 20, 30);
            size = Math.Max(20, Math.Min(300, size)); // Limit size between 20 and 300 sq meters

            int quality = RandomNumberGenerator.Next(1, 11); // Quality from 1 to 10

            HouseType type = DetermineHouseType();

            double price = CalculateInitialPrice(size, quality, type);

            House newHouse = new House(size, quality, type, price);
            houses.Add(newHouse);

            if (i % 10000 == 0)
            {
                Console.WriteLine($"  Created {i} houses");
            }
        }

        Console.WriteLine("Assigning initial housing");
        AssignInitialHousing();
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
        double basePrice = size * 2000; // Base price of 2000 per square meter
        double qualityMultiplier = 0.5 + (quality * 0.1); // Quality affects price
        double typeMultiplier = type == HouseType.Buy ? 1.2 : 1.0; // Buying is more expensive than renting

        return basePrice * qualityMultiplier * typeMultiplier;
    }

    private void AssignInitialHousing()
    {
        List<House> availableHouses = new List<House>(houses);

        foreach (var household in households)
        {
            if (availableHouses.Count == 0) break; // No more houses available

            // Find a suitable house based on household size
            House suitableHouse = availableHouses
                .OrderBy(h => Math.Abs(h.Size - household.Members.Count * 20))
                .First();

            // Assign the house to the household
            suitableHouse.ChangeOwnership(household);
            household.Contract = new HousingContract(
                suitableHouse.Type == HouseType.Buy ? ContractType.OwnerOccupiedWithoutLoan : ContractType.RentedFromLandlord,
                suitableHouse
            );

            availableHouses.Remove(suitableHouse);
        }
    }

    private void OutputMonthlyStatistics(int year, int month)
    {
        int row = (year * 12) + month + 2; // +2 because row 1 is headers

        var stats = CalculateStatistics();

        worksheet.Cells[row, 1].Value = year + 1;
        worksheet.Cells[row, 2].Value = month + 1;
        worksheet.Cells[row, 3].Value = stats.TotalHouseholds;
        worksheet.Cells[row, 4].Value = stats.TotalPopulation;
        worksheet.Cells[row, 5].Value = stats.AverageHouseholdSize;
        worksheet.Cells[row, 6].Value = stats.TotalHouses;
        worksheet.Cells[row, 7].Value = stats.VacantHouses;
        worksheet.Cells[row, 8].Value = stats.AverageHousePrice;
        worksheet.Cells[row, 9].Value = stats.AverageRent;
        worksheet.Cells[row, 10].Value = stats.HomeownershipRate;
        worksheet.Cells[row, 11].Value = stats.AverageIncome;
        worksheet.Cells[row, 12].Value = stats.UnemploymentRate;
        worksheet.Cells[row, 13].Value = stats.HouseholdsWantingToMove;
        worksheet.Cells[row, 14].Value = stats.MeanAreaPerPerson; 

        // Save after each month to ensure data is not lost if the simulation crashes
        excelPackage.Save();

        // Print some key statistics to the console
        Console.WriteLine($"    Month {month + 1} stats: Population: {stats.TotalPopulation}, Households: {stats.TotalHouseholds}, Avg House Price: {stats.AverageHousePrice:C}, Households Wanting to Move: {stats.HouseholdsWantingToMove}, Mean Area per Person: {stats.MeanAreaPerPerson:F2}");
    }

    private void OutputYearlyStatistics(int year, int month)
    {
        var stats = CalculateStatistics();

        var yearlySheet = excelPackage.Workbook.Worksheets.FirstOrDefault(ws => ws.Name == "Yearly Statistics")
                          ?? excelPackage.Workbook.Worksheets.Add("Yearly Statistics");

        int row = year + 2; // +2 because row 1 is headers

        yearlySheet.Cells[row, 1].Value = year + 1;
        yearlySheet.Cells[row, 2].Value = stats.TotalHouseholds;
        yearlySheet.Cells[row, 3].Value = stats.TotalPopulation;
        yearlySheet.Cells[row, 4].Value = stats.AverageHouseholdSize;
        yearlySheet.Cells[row, 5].Value = stats.TotalHouses;
        yearlySheet.Cells[row, 6].Value = stats.VacantHouses;
        yearlySheet.Cells[row, 7].Value = stats.AverageHousePrice;
        yearlySheet.Cells[row, 8].Value = stats.AverageRent;
        yearlySheet.Cells[row, 9].Value = stats.HomeownershipRate;
        yearlySheet.Cells[row, 10].Value = stats.AverageIncome;
        yearlySheet.Cells[row, 11].Value = stats.UnemploymentRate;
        yearlySheet.Cells[row, 12].Value = stats.HouseholdsWantingToMove;
        yearlySheet.Cells[row, 13].Value = stats.MeanAreaPerPerson;

        // Print yearly statistics to console
        Console.WriteLine($"Year {year + 1} Summary:");
        Console.WriteLine($"  Population: {stats.TotalPopulation}");
        Console.WriteLine($"  Households: {stats.TotalHouseholds}");
        Console.WriteLine($"  Avg House Price: {stats.AverageHousePrice:C}");
        Console.WriteLine($"  Homeownership Rate: {stats.HomeownershipRate:P2}");
        Console.WriteLine($"  Unemployment Rate: {stats.UnemploymentRate:P2}");
        Console.WriteLine($"  Mean Area per Person: {stats.MeanAreaPerPerson:F2}");

        // Save after each year
        excelPackage.Save();
    }

    private void LogSimulationState(int year, int month)
    {
        Console.WriteLine($"Year {year + 1}, Month {month + 1} State:");
        Console.WriteLine($"  Total Households: {households.Count}");
        Console.WriteLine($"  Total Houses: {houses.Count}");
        Console.WriteLine($"  Available Houses: {houses.Count(h => h.IsAvailable())}");
        Console.WriteLine($"  Households wanting to move: {households.Count(h => h.WantToMove)}");
    }

    private SimulationStatistics CalculateStatistics()
    {
        double totalOccupiedArea = households.Sum(h => h.Contract?.House?.Size ?? 0);
        int totalPopulation = households.Sum(h => h.Members.Count);
        double meanAreaPerPerson = totalPopulation > 0 ? totalOccupiedArea / totalPopulation : 0;

        return new SimulationStatistics
        {
            TotalHouseholds = households.Count,
            TotalPopulation = totalPopulation,
            AverageHouseholdSize = households.Average(h => h.Members.Count),
            TotalHouses = houses.Count,
            VacantHouses = houses.Count(h => h.IsAvailable()),
            AverageHousePrice = houses.Where(h => h.Type == HouseType.Buy).Average(h => h.Price),
            AverageRent = houses.Where(h => h.Type == HouseType.PrivateRent).Average(h => h.Price),
            HomeownershipRate = (double)households.Count(h => h.Contract.Type == ContractType.OwnerOccupiedWithLoan || h.Contract.Type == ContractType.OwnerOccupiedWithoutLoan) / households.Count,
            AverageIncome = households.Average(h => h.TotalIncome),
            UnemploymentRate = (double)households.Sum(h => h.Members.Count(m => m.Income == 0)) / totalPopulation,
            HouseholdsWantingToMove = households.Count(h => h.WantToMove),
            MeanAreaPerPerson = meanAreaPerPerson 
        };
    }

    public class SimulationStatistics
    {
        public int HouseholdsWantingToMove { get; set; }
        public double MeanAreaPerPerson { get; set; }
        public int TotalHouseholds { get; set; }
        public int TotalPopulation { get; set; }
        public double AverageHouseholdSize { get; set; }
        public int TotalHouses { get; set; }
        public int VacantHouses { get; set; }
        public double AverageHousePrice { get; set; }
        public double AverageRent { get; set; }
        public double HomeownershipRate { get; set; }
        public double AverageIncome { get; set; }
        public double UnemploymentRate { get; set; }
    }

    public void Dispose()
    {
        if (excelPackage != null)
        {
            excelPackage.Save();
            excelPackage.Dispose();
        }
    }
}