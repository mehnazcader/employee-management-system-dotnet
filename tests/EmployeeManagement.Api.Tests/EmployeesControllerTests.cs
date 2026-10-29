
using EmployeeManagement.Api.Controllers;
using EmployeeManagement.Api.Data;
using EmployeeManagement.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EmployeeManagement.Api.Tests;

public class EmployeesControllerTests
{
    //creates a separate temp db for each test to ensure isolation and prevent interference between tests.
    // Create an independent in-memory database for every test.
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    // Create a sample employee with a VALID department/job title.
    private static Employee CreateSampleEmployee()
    {
        return new Employee
        {
            FullName = "Sara Ahmed",
            Email = "sara@example.com",
            Department = "Information Technology",
            JobTitle = "Software Engineer",
            IsActive = true
        };
    }

    // --------------------------------------------------
    // GET ALL EMPLOYEES
    // --------------------------------------------------
    //fact tells xUnit that this method is an automated test
    [Fact]
    // Check that GET returns the employee list with a 200 OK response.
    public async Task GetEmployees_ReturnsOkWithEmployees()
    {
        using var context = CreateDbContext();

        context.Employees.Add(CreateSampleEmployee());
        await context.SaveChangesAsync();

        var controller = new EmployeesController(context);

        var result = await controller.GetEmployees();
        //Checks whether the actual result matches the expected result.
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var employees =
            Assert.IsAssignableFrom<IEnumerable<Employee>>(okResult.Value);

        Assert.Single(employees);
    }

    // --------------------------------------------------
    // GET ONE EXISTING EMPLOYEE
    // --------------------------------------------------
    [Fact]
    // Check that GET finds an existing employee by ID and returns their details.
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

    // --------------------------------------------------
    // GET AN EMPLOYEE THAT DOES NOT EXIST
    // --------------------------------------------------
    // Check that GET returns 404 Not Found when the employee ID does not exist.
    [Fact]

    public async Task GetEmployee_InvalidId_ReturnsNotFound()
    {
        using var context = CreateDbContext();
        var controller = new EmployeesController(context);

        var result = await controller.GetEmployee(999);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    // --------------------------------------------------
    // CREATE A VALID EMPLOYEE
    // --------------------------------------------------
    // Check that POST creates a valid employee, returns 201 Created, and generates an ID.
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

    // --------------------------------------------------
    // REJECT A DUPLICATE EMAIL
    // --------------------------------------------------
    // Check that POST rejects a duplicate email with 409 Conflict and does not save the new employee.
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

    // --------------------------------------------------
    // UPDATE AN EXISTING EMPLOYEE
    // --------------------------------------------------
    // Check that PUT updates an existing employee, saves the changes, and returns 204 No Content.
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
            Department = "Human Resources",
            JobTitle = "HR Manager",
            IsActive = false
        };

        var result =
            await controller.UpdateEmployee(employee.Id, updatedEmployee);

        Assert.IsType<NoContentResult>(result);

        var savedEmployee =
            await context.Employees.FindAsync(employee.Id);

        Assert.NotNull(savedEmployee);
        Assert.Equal("Sara Updated", savedEmployee.FullName);
        Assert.Equal("Human Resources", savedEmployee.Department);
        Assert.Equal("HR Manager", savedEmployee.JobTitle);
        Assert.False(savedEmployee.IsActive);
    }

    // --------------------------------------------------
    // UPDATE AN EMPLOYEE THAT DOES NOT EXIST
    // --------------------------------------------------
    // Check that PUT returns 404 Not Found when the employee ID does not exist.
    [Fact]
    public async Task UpdateEmployee_InvalidId_ReturnsNotFound()
    {
        using var context = CreateDbContext();
        var controller = new EmployeesController(context);

        var result =
            await controller.UpdateEmployee(999, CreateSampleEmployee());

        Assert.IsType<NotFoundResult>(result);
    }

    // --------------------------------------------------
    // REJECT A DUPLICATE EMAIL DURING UPDATE
    // --------------------------------------------------
    // Check that PUT returns 409 Conflict when updating an employee with another employee's email.
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

        var result = await controller.UpdateEmployee(
            secondEmployee.Id,
            updatedEmployee);

        Assert.IsType<ConflictObjectResult>(result);
    }

    // --------------------------------------------------
    // DELETE AN EXISTING EMPLOYEE
    // --------------------------------------------------
    // Check that DELETE removes an existing employee and returns 204 No Content.
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

    // --------------------------------------------------
    // DELETE AN EMPLOYEE THAT DOES NOT EXIST
    // --------------------------------------------------
    // Check that DELETE returns 404 Not Found when the employee ID does not exist.
    [Fact]
    public async Task DeleteEmployee_InvalidId_ReturnsNotFound()
    {
        using var context = CreateDbContext();
        var controller = new EmployeesController(context);

        var result = await controller.DeleteEmployee(999);

        Assert.IsType<NotFoundResult>(result);
    }

    // ==================================================
    // NEW TESTS: DEPARTMENT AND JOB TITLE VALIDATION
    // ==================================================

    // --------------------------------------------------
    // POST: Finance cannot have a Software Developer.
    // --------------------------------------------------
    // Check that POST rejects an invalid department and job title with 400 Bad Request.
    [Fact]
    public async Task CreateEmployee_InvalidJobForDepartment_ReturnsBadRequest()
    {
        using var context = CreateDbContext();
        var controller = new EmployeesController(context);

        var employee = new Employee
        {
            FullName = "Test Employee",
            Email = "test@example.com",
            Department = "Finance",
            JobTitle = "Software Developer",
            IsActive = true
        };

        var result = await controller.CreateEmployee(employee);

        Assert.IsType<BadRequestObjectResult>(result.Result);

        // Invalid employee must NOT be saved.
        Assert.Empty(context.Employees);
    }

    // --------------------------------------------------
    // POST: Reject departments not in the approved list.
    // --------------------------------------------------
    // Check that POST rejects an unknown department with 400 Bad Request and saves nothing.
    [Fact]
    public async Task CreateEmployee_UnknownDepartment_ReturnsBadRequest()
    {
        using var context = CreateDbContext();
        var controller = new EmployeesController(context);

        var employee = new Employee
        {
            FullName = "Test Employee",
            Email = "unknown@example.com",
            Department = "Unknown Department",
            JobTitle = "Software Engineer",
            IsActive = true
        };

        var result = await controller.CreateEmployee(employee);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Empty(context.Employees);
    }

    // --------------------------------------------------
    // POST: Reject job titles not in the approved list.
    // --------------------------------------------------
    // Check that POST rejects an unknown job title with 400 Bad Request and saves nothing.
    [Fact]
    public async Task CreateEmployee_UnknownJobTitle_ReturnsBadRequest()
    {
        using var context = CreateDbContext();
        var controller = new EmployeesController(context);

        var employee = new Employee
        {
            FullName = "Test Employee",
            Email = "unknownjob@example.com",
            Department = "Finance",
            JobTitle = "Astronaut",
            IsActive = true
        };

        var result = await controller.CreateEmployee(employee);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Empty(context.Employees);
    }

    // --------------------------------------------------
    // PUT: Reject an invalid department/job title pair.
    // Also confirm the saved employee is not modified.
    // --------------------------------------------------
    // Check that PUT rejects an invalid department/job title with 400 Bad Request and keeps the original employee unchanged.
    [Fact]
    public async Task UpdateEmployee_InvalidJobForDepartment_ReturnsBadRequest()
    {
        using var context = CreateDbContePxt();

        var employee = CreateSampleEmployee();
        context.Employees.Add(employee);
        await context.SaveChangesAsync();

        var controller = new EmployeesController(context);

        var updatedEmployee = new Employee
        {
            FullName = "Sara Updated",
            Email = "sara.updated@example.com",
            Department = "Finance",
            JobTitle = "Software Developer",
            IsActive = false
        };

        var result = await controller.UpdateEmployee(
            employee.Id,
            updatedEmployee);

        Assert.IsType<BadRequestObjectResult>(result);

        // The original employee should remain unchanged.
        var savedEmployee =
            await context.Employees.FindAsync(employee.Id);

        Assert.NotNull(savedEmployee);
        Assert.Equal("Sara Ahmed", savedEmployee.FullName);
        Assert.Equal("Information Technology", savedEmployee.Department);
        Assert.Equal("Software Engineer", savedEmployee.JobTitle);
        Assert.True(savedEmployee.IsActive);
    }
}
