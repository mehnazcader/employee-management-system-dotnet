
using EmployeeManagement.Api.Data;
using EmployeeManagement.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeesController : ControllerBase
{
    private readonly AppDbContext _context;

    // These are the same departments and job titles used in React.
    // The backend checks them independently, so requests sent through
    // Postman cannot bypass the rule enforced by the dropdowns.
    private static readonly Dictionary<string, string[]> DepartmentJobs =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Information Technology"] =
            [
                "Software Developer",
                "Software Engineer",
                "IT Support Specialist",
                "System Administrator",
                "QA Engineer"
            ],
            ["Human Resources"] =
            [
                "HR Officer",
                "HR Manager",
                "Recruitment Specialist",
                "HR Assistant"
            ],
            ["Finance"] =
            [
                "Accountant",
                "Financial Analyst",
                "Finance Officer",
                "Finance Manager"
            ],
            ["Marketing"] =
            [
                "Marketing Executive",
                "Digital Marketing Specialist",
                "Marketing Manager",
                "Content Specialist"
            ],
            ["Operations"] =
            [
                "Operations Coordinator",
                "Operations Manager",
                "Operations Analyst",
                "Administrative Officer"
            ],
            ["Sales"] =
            [
                "Sales Executive",
                "Sales Representative",
                "Sales Manager",
                "Account Manager"
            ]
        };

    public EmployeesController(AppDbContext context)
    {
        _context = context;
    }

    // Check that the selected job title belongs to the department.
    // Return an error message when the combination is invalid.
    private static string? ValidateDepartmentAndJobTitle(
        string? department,
        string? jobTitle)
    {
        if (string.IsNullOrWhiteSpace(department) ||
            !DepartmentJobs.TryGetValue(
                department.Trim(),
                out var allowedJobs))
        {
            return "Please select a valid department.";
        }

        if (string.IsNullOrWhiteSpace(jobTitle) ||
            !allowedJobs.Contains(
                jobTitle.Trim(),
                StringComparer.OrdinalIgnoreCase))
        {
            return
                $"The job title '{jobTitle}' is not valid for " +
                $"the '{department}' department.";
        }

        return null;
    }

    // GET: api/employees
    // Retrieve all employees.
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Employee>>> GetEmployees()
    {
        var employees = await _context.Employees.ToListAsync();

        return Ok(employees);
    }

    // GET: api/employees/2
    // Retrieve one employee by ID.
    [HttpGet("{id}")]
    public async Task<ActionResult<Employee>> GetEmployee(int id)
    {
        var employee = await _context.Employees.FindAsync(id);

        if (employee == null)
        {
            return NotFound();
        }

        return Ok(employee);
    }

    // POST: api/employees
    // Create a new employee.
    [HttpPost]
    public async Task<ActionResult<Employee>> CreateEmployee(
        Employee employee)
    {
        // Reject invalid departments and job titles before saving.
        string? validationError = ValidateDepartmentAndJobTitle(
            employee.Department,
            employee.JobTitle);

        if (validationError != null)
        {
            return BadRequest(validationError);
        }

        // Check for an existing employee with the same email.
        bool emailExists = await _context.Employees
            .AnyAsync(e => e.Email == employee.Email);

        if (emailExists)
        {
            return Conflict(
                "An employee with this email already exists.");
        }

        _context.Employees.Add(employee);

        // SQL Server generates the employee ID.
        await _context.SaveChangesAsync();

        return Created(
            $"/api/employees/{employee.Id}",
            employee);
    }

    // PUT: api/employees/2
    // Update an existing employee.
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateEmployee(
        int id,
        Employee updatedEmployee)
    {
        // First check whether the employee exists.
        var employee = await _context.Employees.FindAsync(id);

        if (employee == null)
        {
            return NotFound();
        }

        // Reject invalid department/job title combinations.
        string? validationError = ValidateDepartmentAndJobTitle(
            updatedEmployee.Department,
            updatedEmployee.JobTitle);

        if (validationError != null)
        {
            return BadRequest(validationError);
        }

        // Check for an email used by ANOTHER employee.
        bool emailExists = await _context.Employees
            .AnyAsync(e =>
                e.Email == updatedEmployee.Email &&
                e.Id != id);

        if (emailExists)
        {
            return Conflict(
                "An employee with this email already exists.");
        }

        // Update the fields, keeping the original employee ID.
        employee.FullName = updatedEmployee.FullName;
        employee.Email = updatedEmployee.Email;
        employee.Department = updatedEmployee.Department;
        employee.JobTitle = updatedEmployee.JobTitle;
        employee.IsActive = updatedEmployee.IsActive;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/employees/2
    // Delete an employee.
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEmployee(int id)
    {
        var employee = await _context.Employees.FindAsync(id);

        if (employee == null)
        {
            return NotFound();
        }

        _context.Employees.Remove(employee);

        await _context.SaveChangesAsync();

        return NoContent();
    }
}
