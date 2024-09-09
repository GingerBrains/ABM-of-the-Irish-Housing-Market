using irish_housing_abm;
using OfficeOpenXml;

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
        populationModel = new PopulationModel();
        householdDataStore = new HouseholdDataStore(); // Make sure to initialize this
        bank = new Bank(0.03); // 3% interest rate
        constructionCompany = new ConstructionCompany(1000); // Scale factor of 1000
        simulationUpdater = new SimulationUpdater(bank, constructionCompany, populationModel);
        houses = new List<House>();

        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        outputPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "SimulationResults.xlsx");
        excelPackage = new ExcelPackage(new FileInfo(outputPath));
        worksheet = excelPackage.Workbook.Worksheets.Add("Monthly Statistics");
        InitializeExcelHeader();
    }

    private void InitializeExcelHeader()
    {
        string[] headers = new string[]
        {
            "Year", "Total Households", "Total Population", "Average Household Size",
            "Total Houses", "Vacant Houses", "Average House Price", "Average Rent",
            "Homeownership Rate", "Average Income", "Unemployment Rate"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cells[1, i + 1].Value = headers[i];
        }
    }

    public void Run(int years, double scaleFactor)
    {
        Initialize(scaleFactor);

        for (int year = 0; year < years; year++)
        {
            Console.WriteLine($"Starting year {year + 1}");
            LogSimulationState(year, 0);

            for (int month = 0; month < 12; month++)
            {
                Console.WriteLine($"  Processing month {month + 1}");
                simulationUpdater.PerformMonthlyUpdates(households, houses);
                Console.WriteLine($"  Completed month {month + 1}");
            }


            Console.WriteLine("Starting yearly updates");
            double taxRate = 0.2; // Example tax rate
            simulationUpdater.PerformYearlyUpdates(households, houses, ref taxRate);
            Console.WriteLine("Completed yearly updates");

            OutputYearlyStatistics(year);

            Console.WriteLine($"Completed year {year + 1}");
        }

        // Finalize the Excel file
        worksheet.Cells.AutoFitColumns();
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

    private void OutputYearlyStatistics(int year)
    {
        int row = year + 2; // +2 because row 1 is headers

        var stats = CalculateStatistics();

        worksheet.Cells[row, 1].Value = year + 1; // Year number (1-based)
        worksheet.Cells[row, 2].Value = stats.TotalHouseholds;
        worksheet.Cells[row, 3].Value = stats.TotalPopulation;
        worksheet.Cells[row, 4].Value = stats.AverageHouseholdSize;
        worksheet.Cells[row, 5].Value = stats.TotalHouses;
        worksheet.Cells[row, 6].Value = stats.VacantHouses;
        worksheet.Cells[row, 7].Value = stats.AverageHousePrice;
        worksheet.Cells[row, 8].Value = stats.AverageRent;
        worksheet.Cells[row, 9].Value = stats.HomeownershipRate;
        worksheet.Cells[row, 10].Value = stats.AverageIncome;
        worksheet.Cells[row, 11].Value = stats.UnemploymentRate;

        // Save after each year to ensure data is not lost if the simulation crashes
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
        return new SimulationStatistics
        {
            TotalHouseholds = households.Count,
            TotalPopulation = households.Sum(h => h.Members.Count),
            AverageHouseholdSize = households.Average(h => h.Members.Count),
            TotalHouses = houses.Count,
            VacantHouses = houses.Count(h => h.IsAvailable()),
            AverageHousePrice = houses.Where(h => h.Type == HouseType.Buy).Average(h => h.Price),
            AverageRent = houses.Where(h => h.Type == HouseType.PrivateRent).Average(h => h.Price),
            HomeownershipRate = (double)households.Count(h => h.Contract.Type == ContractType.OwnerOccupiedWithLoan || h.Contract.Type == ContractType.OwnerOccupiedWithoutLoan) / households.Count,
            AverageIncome = households.Average(h => h.TotalIncome),
            UnemploymentRate = (double)households.Sum(h => h.Members.Count(m => m.Income == 0)) / households.Sum(h => h.Members.Count)
        };
    }

    public class SimulationStatistics
    {
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