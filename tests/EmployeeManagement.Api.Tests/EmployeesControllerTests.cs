
using EmployeeManagement.Api.Controllers;
using EmployeeManagement.Api.Data;
using EmployeeManagement.Api.Models;
using EmployeeManagement.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EmployeeManagement.Api.Tests;

// Unit tests for the employee controller.
// Each test uses a separate in-memory database.
public class EmployeesControllerTests
{
    // --------------------------------------------------
    // TEST DATABASE
    // --------------------------------------------------

    // Create an independent database for every test.
    // This prevents one test from affecting another.
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    // Create a controller with both required dependencies.
    // The Excel service is needed because the controller
    // now supports exporting employees.
    private static EmployeesController CreateController(
        AppDbContext context)
    {
        return new EmployeesController(
            context,
            new EmployeeExcelService()
        );
    }

    // Create a sample employee for testing.
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

    // ==================================================
    // TEST 1: GET ALL EMPLOYEES
    // ==================================================

    [Fact]
    public async Task GetEmployees_ReturnsOkWithEmployees()
    {
        using var context = CreateDbContext();

        context.Employees.Add(CreateSampleEmployee());
        await context.SaveChangesAsync();

        var controller = CreateController(context);

        var result = await controller.GetEmployees();

        // GET returns an ActionResult containing an OkObjectResult.
        var okResult =
            Assert.IsType<OkObjectResult>(result.Result);

        var employees =
            Assert.IsAssignableFrom<IEnumerable<Employee>>(
                okResult.Value
            );

        Assert.Single(employees);
    }

    // ==================================================
    // TEST 2: GET AN EXISTING EMPLOYEE
    // ==================================================

    [Fact]
    public async Task GetEmployee_ExistingId_ReturnsOk()
    {
        using var context = CreateDbContext();

        var employee = CreateSampleEmployee();

        context.Employees.Add(employee);
        await context.SaveChangesAsync();

        var controller = CreateController(context);

        var result = await controller.GetEmployee(employee.Id);

        var okResult =
            Assert.IsType<OkObjectResult>(result.Result);

        var returnedEmployee =
            Assert.IsType<Employee>(okResult.Value);

        Assert.Equal(
            "Sara Ahmed",
            returnedEmployee.FullName
        );
    }

    // ==================================================
    // TEST 3: GET AN EMPLOYEE THAT DOES NOT EXIST
    // ==================================================

    [Fact]
    public async Task GetEmployee_InvalidId_ReturnsNotFound()
    {
        using var context = CreateDbContext();

        var controller = CreateController(context);

        var result = await controller.GetEmployee(999);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    // ==================================================
    // TEST 4: CREATE A VALID EMPLOYEE
    // ==================================================

    [Fact]
    public async Task CreateEmployee_ValidEmployee_ReturnsCreated()
    {
        using var context = CreateDbContext();

        var controller = CreateController(context);

        var employee = CreateSampleEmployee();

        var result = await controller.CreateEmployee(employee);

        // The controller uses CreatedAtAction().
        // This represents an HTTP 201 Created response.
        var createdResult =
            Assert.IsType<CreatedAtActionResult>(result);

        Assert.Equal(201, createdResult.StatusCode);

        // Check that the employee was saved.
        Assert.Single(context.Employees);

        // SQL Server normally generates this ID.
        // The EF Core InMemory provider generates one for testing.
        Assert.True(employee.Id > 0);
    }

    // ==================================================
    // TEST 5: REJECT A DUPLICATE EMAIL
    // ==================================================

    [Fact]
    public async Task CreateEmployee_DuplicateEmail_ReturnsConflict()
    {
        using var context = CreateDbContext();

        context.Employees.Add(CreateSampleEmployee());
        await context.SaveChangesAsync();

        var controller = CreateController(context);

        var duplicateEmployee = CreateSampleEmployee();

        duplicateEmployee.FullName = "Another Employee";

        var result =
            await controller.CreateEmployee(duplicateEmployee);

        // A duplicate email should produce HTTP 409 Conflict.
        Assert.IsType<ConflictObjectResult>(result);

        // No additional employee should be saved.
        Assert.Single(context.Employees);
    }

    // ==================================================
    // TEST 6: UPDATE AN EXISTING EMPLOYEE
    // ==================================================

    [Fact]
    public async Task UpdateEmployee_ExistingId_ReturnsNoContent()
    {
        using var context = CreateDbContext();

        var employee = CreateSampleEmployee();

        context.Employees.Add(employee);
        await context.SaveChangesAsync();

        var controller = CreateController(context);

        var updatedEmployee = new Employee
        {
            FullName = "Sara Updated",
            Email = "sara.updated@example.com",
            Department = "Human Resources",
            JobTitle = "HR Manager",
            IsActive = false
        };

        var result = await controller.UpdateEmployee(
            employee.Id,
            updatedEmployee
        );

        // Successful updates return HTTP 204 No Content.
        Assert.IsType<NoContentResult>(result);

        // Retrieve the employee to verify the saved changes.
        var savedEmployee =
            await context.Employees.FindAsync(employee.Id);

        Assert.NotNull(savedEmployee);

        Assert.Equal(
            "Sara Updated",
            savedEmployee.FullName
        );

        Assert.Equal(
            "sara.updated@example.com",
            savedEmployee.Email
        );

        Assert.Equal(
            "Human Resources",
            savedEmployee.Department
        );

        Assert.Equal(
            "HR Manager",
            savedEmployee.JobTitle
        );

        Assert.False(savedEmployee.IsActive);
    }

    // ==================================================
    // TEST 7: UPDATE A NONEXISTENT EMPLOYEE
    // ==================================================

    [Fact]
    public async Task UpdateEmployee_InvalidId_ReturnsNotFound()
    {
        using var context = CreateDbContext();

        var controller = CreateController(context);

        var result = await controller.UpdateEmployee(
            999,
            CreateSampleEmployee()
        );

        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ==================================================
    // TEST 8: REJECT DUPLICATE EMAIL DURING UPDATE
    // ==================================================

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

        context.Employees.AddRange(
            firstEmployee,
            secondEmployee
        );

        await context.SaveChangesAsync();

        var controller = CreateController(context);

        // Try changing Emma's email to Sara's existing email.
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
            updatedEmployee
        );

        Assert.IsType<ConflictObjectResult>(result);

        // There should still be only two employees.
        Assert.Equal(2, context.Employees.Count());
    }

    // ==================================================
    // TEST 9: DELETE AN EXISTING EMPLOYEE
    // ==================================================

    [Fact]
    public async Task DeleteEmployee_ExistingId_ReturnsNoContent()
    {
        using var context = CreateDbContext();

        var employee = CreateSampleEmployee();

        context.Employees.Add(employee);
        await context.SaveChangesAsync();

        var controller = CreateController(context);

        var result =
            await controller.DeleteEmployee(employee.Id);

        Assert.IsType<NoContentResult>(result);

        // The employee should no longer exist.
        Assert.Empty(context.Employees);
    }

    // ==================================================
    // TEST 10: DELETE A NONEXISTENT EMPLOYEE
    // ==================================================

    [Fact]
    public async Task DeleteEmployee_InvalidId_ReturnsNotFound()
    {
        using var context = CreateDbContext();

        var controller = CreateController(context);

        var result = await controller.DeleteEmployee(999);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ==================================================
    // DEPARTMENT AND JOB TITLE VALIDATION TESTS
    // These verify business rules, not just required fields.
    // ==================================================

    // ==================================================
    // TEST 11: REJECT INVALID DEPARTMENT/JOB COMBINATION
    // ==================================================

    [Fact]
    public async Task CreateEmployee_InvalidJobForDepartment_ReturnsBadRequest()
    {
        using var context = CreateDbContext();

        var controller = CreateController(context);

        var employee = new Employee
        {
            FullName = "Test Employee",
            Email = "test@example.com",
            Department = "Finance",
            JobTitle = "Software Developer",
            IsActive = true
        };

        var result = await controller.CreateEmployee(employee);

        // Software Developer is not a valid Finance job.
        Assert.IsType<BadRequestObjectResult>(result);

        // Invalid employee must not be saved.
        Assert.Empty(context.Employees);
    }

    // ==================================================
    // TEST 12: REJECT UNKNOWN DEPARTMENT
    // ==================================================

    [Fact]
    public async Task CreateEmployee_UnknownDepartment_ReturnsBadRequest()
    {
        using var context = CreateDbContext();

        var controller = CreateController(context);

        var employee = new Employee
        {
            FullName = "Test Employee",
            Email = "unknown@example.com",
            Department = "Unknown Department",
            JobTitle = "Software Engineer",
            IsActive = true
        };

        var result = await controller.CreateEmployee(employee);

        // Unknown departments should not be accepted.
        Assert.IsType<BadRequestObjectResult>(result);

        Assert.Empty(context.Employees);
    }

    // ==================================================
    // TEST 13: REJECT UNKNOWN JOB TITLE
    // ==================================================

    [Fact]
    public async Task CreateEmployee_UnknownJobTitle_ReturnsBadRequest()
    {
        using var context = CreateDbContext();

        var controller = CreateController(context);

        var employee = new Employee
        {
            FullName = "Test Employee",
            Email = "unknownjob@example.com",
            Department = "Finance",
            JobTitle = "Astronaut",
            IsActive = true
        };

        var result = await controller.CreateEmployee(employee);

        // Astronaut is not an approved Finance job title.
        Assert.IsType<BadRequestObjectResult>(result);

        Assert.Empty(context.Employees);
    }

    // ==================================================
    // TEST 14: REJECT INVALID JOB DURING UPDATE
    // ==================================================

    [Fact]
    public async Task UpdateEmployee_InvalidJobForDepartment_ReturnsBadRequest()
    {
        // Fixed the original CreateDbContePxt typo.
        using var context = CreateDbContext();

        var employee = CreateSampleEmployee();

        context.Employees.Add(employee);
        await context.SaveChangesAsync();

        var controller = CreateController(context);

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
            updatedEmployee
        );

        // Reject an invalid department/job title combination.
        Assert.IsType<BadRequestObjectResult>(result);

        // Verify that the original employee remains unchanged.
        var savedEmployee =
            await context.Employees.FindAsync(employee.Id);

        Assert.NotNull(savedEmployee);

        Assert.Equal(
            "Sara Ahmed",
            savedEmployee.FullName
        );

        Assert.Equal(
            "Information Technology",
            savedEmployee.Department
        );

        Assert.Equal(
            "Software Engineer",
            savedEmployee.JobTitle
        );

        Assert.True(savedEmployee.IsActive);
    }
}
