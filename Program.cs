namespace SampleApp;

public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("=== Sample App starting ===");

        var service = new EmployeeService();

        var emp1 = service.CreateEmployee("Alice", "alice@acme.example", "IT", 45000);
        var emp2 = service.CreateEmployee("Bob",   "bob@acme.example",   "SALES", 38000);

        Console.WriteLine($"Created {emp1.Id} and {emp2.Id}");

        var bonus1 = service.CalculateBonus(emp1);
        var bonus2 = service.CalculateBonus(emp2);

        Console.WriteLine($"Bonus Alice: {bonus1}, Bob: {bonus2}");

        var found = service.FindByName("Alice");
        Console.WriteLine($"Found: {found?.Name ?? "none"}");

        await Task.CompletedTask;
    }
}
