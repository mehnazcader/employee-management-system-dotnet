// Imports AppDbContext, which allows the controller to access the database.
using EmployeeManagement.Api.Data;

// Imports the Employee model.
using EmployeeManagement.Api.Models;

// Provides ControllerBase, routing attributes and HTTP responses.
using Microsoft.AspNetCore.Mvc;

// Provides asynchronous EF Core methods such as ToListAsync() and AnyAsync().
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagement.Api.Controllers;

// Enables automatic model validation.
// Invalid employee data returns HTTP 400 Bad Request.
[ApiController]

// Sets the base URL for this controller to /api/employees.
[Route("api/[controller]")]
public class EmployeesController : ControllerBase
{
    // Stores the database context used by this controller.
    private readonly AppDbContext _context;

    // ASP.NET Core provides AppDbContext through dependency injection.
    public EmployeesController(AppDbContext context)
    {
        _context = context;
    }

    // --------------------------------------------------
    // GET: api/employees
    // Retrieve all employees.
    // --------------------------------------------------
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Employee>>> GetEmployees()
    {
        // Retrieve all employee records from SQL Server.
        var employees = await _context.Employees.ToListAsync();

        // Return HTTP 200 OK with the employee list.
        return Ok(employees);
    }

    // --------------------------------------------------
    // GET: api/employees/2
    // Retrieve one employee using their ID.
    // --------------------------------------------------
    [HttpGet("{id}")]
    public async Task<ActionResult<Employee>> GetEmployee(int id)
    {
        // Search for an employee using their primary key.
        var employee = await _context.Employees.FindAsync(id);

        // Return HTTP 404 if the employee does not exist.
        if (employee == null)
        {
            return NotFound();
        }

        // Return HTTP 200 OK with the employee information.
        return Ok(employee);
    }

    // --------------------------------------------------
    // POST: api/employees
    // Create a new employee.
    // --------------------------------------------------
    [HttpPost]
    public async Task<ActionResult<Employee>> CreateEmployee(Employee employee)
    {
        // Check whether another employee already uses this email.
        bool emailExists = await _context.Employees
            .AnyAsync(e => e.Email == employee.Email);

        // Reject duplicate emails with HTTP 409 Conflict.
        if (emailExists)
        {
            return Conflict("An employee with this email already exists.");
        }

        // Mark the employee for insertion into the database.
        _context.Employees.Add(employee);

        // Save the employee to SQL Server.
        // SQL Server automatically generates the employee ID.
        await _context.SaveChangesAsync();

        // Return HTTP 201 Created with the new employee details.
        return Created($"/api/employees/{employee.Id}", employee);
    }

    // --------------------------------------------------
    // PUT: api/employees/2
    // Update an existing employee.
    // --------------------------------------------------
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateEmployee(
        int id,
        Employee updatedEmployee)
    {
        // Find the employee we want to update.
        var employee = await _context.Employees.FindAsync(id);

        // Return HTTP 404 if the employee does not exist.
        if (employee == null)
        {
            return NotFound();
        }

        // Check whether ANOTHER employee already uses the new email.
        // e.Id != id excludes the employee currently being updated.
        bool emailExists = await _context.Employees
            .AnyAsync(e => e.Email == updatedEmployee.Email && e.Id != id);

        // Reject duplicate emails with HTTP 409 Conflict.
        if (emailExists)
        {
            return Conflict("An employee with this email already exists.");
        }

        // Update the existing employee's information.
        // We do not change the employee ID.
        employee.FullName = updatedEmployee.FullName;
        employee.Email = updatedEmployee.Email;
        employee.Department = updatedEmployee.Department;
        employee.JobTitle = updatedEmployee.JobTitle;
        employee.IsActive = updatedEmployee.IsActive;

        // Save the updated information to SQL Server.
        await _context.SaveChangesAsync();

        // Return HTTP 204 No Content when the update succeeds.
        return NoContent();
    }

    // --------------------------------------------------
    // DELETE: api/employees/2
    // Delete an employee using their ID.
    // --------------------------------------------------
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEmployee(int id)
    {
        // Find the employee we want to delete.
        var employee = await _context.Employees.FindAsync(id);

        // Return HTTP 404 if the employee does not exist.
        if (employee == null)
        {
            return NotFound();
        }

        // Mark the employee for deletion.
        _context.Employees.Remove(employee);

        // Permanently delete the employee from SQL Server.
        await _context.SaveChangesAsync();

        // Return HTTP 204 No Content when deletion succeeds.
        return NoContent();
    }
}