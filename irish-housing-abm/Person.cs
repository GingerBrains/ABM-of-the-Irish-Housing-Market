public class Person
{
    public int Age { get; set; }
    public string Gender { get; set; }
    public double Income { get; set; }

    public Person(int age, string gender, double income)
    {
        Age = age;
        Gender = gender;
        Income = income;
    }

    public void IncrementAge()
    {
        Age++;
    }

    public void UpdateIncome(double newIncome)
    {
        Income = newIncome;
    }
}