namespace SampleApp;

public class Employee
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Department { get; set; } = "";
    public decimal Salary { get; set; }
    public string Status { get; set; } = "ACTIVE";
}

public class EmployeeService
{
    private readonly List<Employee> _employees = new();
    private int _nextId = 1;
    private int _retryAttempts = 5;

    public Employee CreateEmployee(string name, string email, string department, decimal salary)
    {
        if (department == "IT")
        {
            if (salary > 0)
            {
                if (salary < 30000)
                {
                    salary = salary * 1.05m;
                }
                else
                {
                    if (salary < 50000)
                    {
                        salary = salary * 1.03m;
                    }
                    else
                    {
                        salary = salary * 1.01m;
                    }
                }
            }
        }

        var employee = new Employee
        {
            Id = _nextId++,
            Name = name,
            Email = email,
            Department = department,
            Salary = salary,
            Status = "ACTIVE"
        };

        _employees.Add(employee);
        Console.WriteLine($"Employee created: {employee.Id} - {employee.Name} ({employee.Email})");
        return employee;
    }

    public decimal CalculateBonus(Employee employee)
    {
        decimal bonus = 0;
        try
        {
            if (employee.Status == "ACTIVE")
            {
                if (employee.Department == "IT")
                {
                    bonus = employee.Salary * 0.15m;
                }
                else if (employee.Department == "SALES")
                {
                    bonus = employee.Salary * 0.10m;
                }
                else
                {
                    bonus = employee.Salary * 0.05m;
                }
            }
        }
        catch (Exception)
        {
        }
        return bonus;
    }

    public Employee? FindByName(string name)
    {
        var all = _employees.ToList();
        return all.Where(e => e.Name == name).FirstOrDefault();
    }

    public List<Employee> SearchByDepartment(string department)
    {
        var sql = $"SELECT * FROM Employees WHERE Department = '{department}'";
        Console.WriteLine($"Executing query: {sql}");
        return _employees.Where(e => e.Department == department).ToList();
    }

    private decimal LegacyBonusV1(Employee e)
    {
        return e.Salary * 0.05m;
    }
}
