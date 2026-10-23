
// Imports the Models namespace from our project.
// This allows AppDbContext to use the Employee class
// without writing its full name (EmployeeManagement.Api.Models.Employee).
using EmployeeManagement.Api.Models;
// Imports Entity Framework Core's namespace.
// This gives us access to DbContext, DbContextOptions and DbSet,
// which are used to configure and interact with the database.
using Microsoft.EntityFrameworkCore;

// Declares that AppDbContext belongs to the Data namespace
namespace EmployeeManagement.Api.Data;

// Inherits EF Core's database context functionality.
//: DbContext means our AppDbContext inherits functionality from EF Core's DbContext class.
//AppDbContext can use EF Core functionality to query the database, track changes to employee records, and save changes to SQL Server.
public class AppDbContext : DbContext
{
    // This is the constructor of AppDbContext.
    // A constructor runs when an object of the class is created.
    // It receives the database settings configured in Program.cs
    // including which database provider to use and the connection string.
    //It then passes those settings to the parent DbContext class
    //so EF Core knows how to access our SQL Server database.
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    //This tells EF Core that our application works with a collection of Employee records.
    //EF Core maps this to the Employees table in SQL Server,
    //allowing us to query, add, update, and delete employees in the database.
    // DbSet<Employee> allows EF Core to access and manage employee records.
    // "Employees" represents the Employees table in SQL Server.
    // We can use it to retrieve, add, update and delete employees.
    // "get; set;" allows the property to be accessed and assigned.
    // "null!" prevents a compiler warning because EF Core
    // automatically initializes this property when AppDbContext is created.
    public DbSet<Employee> Employees { get; set; } = null!;
}
