//bridge between your C# backend and SQL Server
//import namespaces
using EmployeeManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

//define database context for the application
namespace EmployeeManagement.Api.Data;

public class AppDbContext : DbContext
{
    // Constructor receives the database configuration from Program.cs.
    public AppDbContext(DbContextOpPtions<AppDbContext> options)
        : base(options)
    {
    }

    // Represents the Employees table in SQL Server.
    public DbSet<Employee> Employees { get; set; } = null!;

    // Configure additional database rules.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        //make email address unique for each employee
        // Each employee must have a unique email address.
        modelBuilder.Entity<Employee>()
            .HasIndex(e => e.Email)
            .IsUnique();
    }
}
