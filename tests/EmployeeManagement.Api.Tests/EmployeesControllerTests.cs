
using EmployeeManagement.Api.Controllers;
using EmployeeManagement.Api.Data;
using EmployeeManagement.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EmployeeManagement.Api.Tests;

public class EmployeesControllerTests
{
    // Create a separate temporary database for each test.
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    // Create a sample employee for testing.
    private static Employee CreateSampleEmployee()
    {
        return new Employee
        {
            FullName = "Sara Ahmed",
            Email = "sara@example.com",
            Department = "IT",
            JobTitle = "Software Engineer",
            IsActive = true
        };
    }

    [Fact]
    public async Task GetEmployees_ReturnsOkWithEmployees()
    {
        using var context = CreateDbContext();

        context.Employees.Add(CreateSampleEmployee());
        await context.SaveChangesAsync();

        var controller = new EmployeesController(context);

        var result = await controller.GetEmployees();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var employees = Assert.IsAssignableFrom<IEnumerable<Employee>>(okResult.Value);

        Assert.Single(employees);
    }

    [Fact]
    public async Task GetEmployee_ExistingId_ReturnsOk()
    {
        using var context = CreateDbContext();

        var employee = CreateSampleEmployee();
        context.Employees.Add(employee);
        await context.SaveChangesAsync();

        var controller = new EmployeesController(context);

        var result = await controller.GetEmployee(employee.Id);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedEmployee = Assert.IsType<Employee>(okResult.Value);

        Assert.Equal("Sara Ahmed", returnedEmployee.FullName);
    }

    [Fact]
    public async Task GetEmployee_InvalidId_ReturnsNotFound()
    {
        using var context = CreateDbContext();
        var controller = new EmployeesController(context);

        var result = await controller.GetEmployee(999);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task CreateEmployee_ValidEmployee_ReturnsCreated()
    {
        using var context = CreateDbContext();
        var controller = new EmployeesController(context);

        var employee = CreateSampleEmployee();

        var result = await controller.CreateEmployee(employee);

        var createdResult = Assert.IsType<CreatedResult>(result.Result);

        Assert.Equal(201, createdResult.StatusCode);
        Assert.Single(context.Employees);
        Assert.True(employee.Id > 0);
    }

    [Fact]
    public async Task CreateEmployee_DuplicateEmail_ReturnsConflict()
    {
        using var context = CreateDbContext();

        context.Employees.Add(CreateSampleEmployee());
        await context.SaveChangesAsync();

        var controller = new EmployeesController(context);

        var duplicateEmployee = CreateSampleEmployee();
        duplicateEmployee.FullName = "Another Employee";

        var result = await controller.CreateEmployee(duplicateEmployee);

        Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Single(context.Employees);
    }

    [Fact]
    public async Task UpdateEmployee_ExistingId_ReturnsNoContent()
    {
        using var context = CreateDbContext();

        var employee = CreateSampleEmployee();
        context.Employees.Add(employee);
        await context.SaveChangesAsync();

        var controller = new EmployeesController(context);

        var updatedEmployee = new Employee
        {
            FullName = "Sara Updated",
            Email = "sara.updated@example.com",
            Department = "HR",
            JobTitle = "HR Manager",
            IsActive = false
        };

        var result = await controller.UpdateEmployee(employee.Id, updatedEmployee);

        Assert.IsType<NoContentResult>(result);

        var savedEmployee = await context.Employees.FindAsync(employee.Id);

        Assert.NotNull(savedEmployee);
        Assert.Equal("Sara Updated", savedEmployee.FullName);
        Assert.Equal("HR", savedEmployee.Department);
        Assert.False(savedEmployee.IsActive);
    }

    [Fact]
    public async Task UpdateEmployee_InvalidId_ReturnsNotFound()
    {
        using var context = CreateDbContext();
        var controller = new EmployeesController(context);

        var result = await controller.UpdateEmployee(999, CreateSampleEmployee());

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task UpdateEmployee_DuplicateEmail_ReturnsConflict()
    {
        using var context = CreateDbContext();

        var firstEmployee = CreateSampleEmployee();

        var secondEmployee = new Employee
        {
            FullName = "Emma Smith",
            Email = "emma@example.com",
            Department = "Finance",
            JobTitle = "Accountant",
            IsActive = true
        };

        context.Employees.AddRange(firstEmployee, secondEmployee);
        await context.SaveChangesAsync();

        var controller = new EmployeesController(context);

        var updatedEmployee = new Employee
        {
            FullName = secondEmployee.FullName,
            Email = firstEmployee.Email,
            Department = secondEmployee.Department,
            JobTitle = secondEmployee.JobTitle,
            IsActive = secondEmployee.IsActive
        };

        var result = await controller.UpdateEmployee(secondEmployee.Id, updatedEmployee);

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task DeleteEmployee_ExistingId_ReturnsNoContent()
    {
        using var context = CreateDbContext();

        var employee = CreateSampleEmployee();
        context.Employees.Add(employee);
        await context.SaveChangesAsync();

        var controller = new EmployeesController(context);

        var result = await controller.DeleteEmployee(employee.Id);

        Assert.IsType<NoContentResult>(result);
        Assert.Empty(context.Employees);
    }

    [Fact]
    public async Task DeleteEmployee_InvalidId_ReturnsNotFound()
    {
        using var context = CreateDbContext();
        var controller = new EmployeesController(context);

        var result = await controller.DeleteEmployee(999);

        Assert.IsType<NotFoundResult>(result);
    }
}
