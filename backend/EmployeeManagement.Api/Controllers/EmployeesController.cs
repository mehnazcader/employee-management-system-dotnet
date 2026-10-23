// Imports the Data namespace from our project.
// Provides AppDbContext.
using EmployeeManagement.Api.Data;
// Imports the Models namespace from our project.
// Provides Employee
using EmployeeManagement.Api.Models;
// Provides ControllerBase, route attributes and HTTP results
using Microsoft.AspNetCore.Mvc;
// Provides ToListAsync(),
// which retrieves employee records asynchronously from SQL Server.
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagement.Api.Controllers;

// Enables API behaviors, including automatic HTTP 400
// responses when model validation fails.
[ApiController]
// Replaces [controller] with Employees, resulting in
// the base route /api/employees.
[Route("api/[controller]")]
public class EmployeesController : ControllerBase
{
    // 'private': only this class can access the field.
    // 'readonly': field cannot be reassigned after construction
    private readonly AppDbContext _context;

    // Constructor injection: ASP.NET Core provides AppDbContext.
    public EmployeesController(AppDbContext context)
    {
        _context = context;
    }

    //View Employees
    // GET: api/employees
    // Retrieve all employees from SQL Server.
    [HttpGet]
    // public: Allows ASP.NET Core to access this method.
    // async/Task: Supports asynchronous operations using await.
    // ActionResult: Allows us to return an HTTP response.
    // IEnumerable<Employee>: Represents a collection of employees.
    public async Task<ActionResult<IEnumerable<Employee>>> GetEmployees()
    {
        // _context is our AppDbContext provided through dependency injection.
        // .Employees represents the employee records in SQL Server.
        // ToListAsync() retrieves all employees as a list.
        // await waits asynchronously for the database query to finish.
        // var employees stores the retrieved records.
        var employees = await _context.Employees.ToListAsync();
        // Returns HTTP 200 OK with employee records in JSON format.
        // If no employees exist, an empty list [] is returned.
        return Ok(employees);
    }

    //Add an employee
    // POST: api/employees
    // Add a new employee to SQL Server.
    [HttpPost]
    // async/Task: Allows asynchronous database operations.
    // ActionResult<Employee>: Returns an HTTP response with employee data.
    // Employee employee: Receives employee information from the JSON request.
    public async Task<ActionResult<Employee>> CreateEmployee(Employee employee)
    {
        // ASP.NET Core automatically validates the Employee model
        // using [Required] and [EmailAddress].
        // Invalid data returns HTTP 400 Bad Request.
        // Duplicate-email checking is not yet implemented.

        // Adds the new employee to EF Core's change tracker.
        // The employee is not saved to SQL Server yet.
        _context.Employees.Add(employee);
        // Saves the employee to SQL Server asynchronously.
        // SQL Server automatically generates the employee's ID.
        await _context.SaveChangesAsync();

        // Returns HTTP 201 Created with the employee information.
        // The URL identifies the new employee, e.g. /api/employees/1.
        return Created(
            $"/api/employees/{employee.Id}",
            employee
        );
    }
}
