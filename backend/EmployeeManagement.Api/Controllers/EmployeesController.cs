
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using EmployeeManagement.Api.Data;
using EmployeeManagement.Api.Models;
using EmployeeManagement.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagement.Api.Controllers;

// Handles employee CRUD operations and Excel export.
[ApiController]
[Route("api/employees")]
public class EmployeesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IEmployeeExcelService _excelService;

    // Dependencies are supplied through dependency injection.
    public EmployeesController(
        AppDbContext context,
        IEmployeeExcelService excelService)
    {
        _context = context;
        _excelService = excelService;
    }

    // Validate names containing Unicode letters.
    // Spaces, hyphens, apostrophes and periods are allowed.
    private static readonly Regex NamePattern = new(
        @"^\p{L}[\p{L}\p{M}]*(?:[ .'-]\p{L}[\p{L}\p{M}]*)*$",
        RegexOptions.Compiled,
        TimeSpan.FromSeconds(1));

    // Approved departments and their corresponding job titles.
    // This protects the backend even if requests bypass React.
    private static readonly Dictionary<string, string[]> DepartmentJobs = new()
    {
        ["Information Technology"] = new[]
        {
            "Software Developer",
            "Software Engineer",
            "IT Support Specialist",
            "System Administrator",
            "QA Engineer"
        },
        ["Human Resources"] = new[]
        {
            "HR Officer",
            "HR Manager",
            "Recruitment Specialist",
            "HR Assistant"
        },
        ["Finance"] = new[]
        {
            "Accountant",
            "Financial Analyst",
            "Finance Officer",
            "Finance Manager"
        },
        ["Marketing"] = new[]
        {
            "Marketing Executive",
            "Digital Marketing Specialist",
            "Marketing Manager",
            "Content Specialist"
        },
        ["Operations"] = new[]
        {
            "Operations Coordinator",
            "Operations Manager",
            "Operations Analyst",
            "Administrative Officer"
        },
        ["Sales"] = new[]
        {
            "Sales Executive",
            "Sales Representative",
            "Sales Manager",
            "Account Manager"
        }
    };

    // GET: api/employees
    // Retrieve all employees for the directory.
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Employee>>> GetEmployees()
    {
        var employees = await _context.Employees
            .AsNoTracking()
            .OrderBy(e => e.Id)
            .ToListAsync();

        return Ok(employees);
    }

    // GET: api/employees/5
    // Retrieve one employee by ID.
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Employee>> GetEmployee(int id)
    {
        var employee = await _context.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id);

        if (employee == null)
        {
            return NotFound("Employee not found.");
        }

        return Ok(employee);
    }

    // POST: api/employees
    // Add a new employee.
    [HttpPost]
    public async Task<IActionResult> CreateEmployee(
        [FromBody] Employee employee)
    {
        // Validate and normalize employee information.
        var validationError = ValidateEmployee(employee);

        if (validationError != null)
        {
            return BadRequest(validationError);
        }

        // Prevent duplicate email addresses.
        var emailExists = await _context.Employees
            .AnyAsync(e => e.Email.ToLower() == employee.Email);

        if (emailExists)
        {
            return Conflict(
                "An employee with this email already exists.");
        }

        // SQL Server generates the new employee ID.
        employee.Id = 0;

        _context.Employees.Add(employee);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(
                "The employee could not be saved. " +
                "The email may already exist.");
        }

        return CreatedAtAction(
            nameof(GetEmployee),
            new { id = employee.Id },
            employee);
    }

    // PUT: api/employees/5
    // Update an existing employee.
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateEmployee(
        int id,
        [FromBody] Employee updatedEmployee)
    {
        // Validate the new values before changing the database.
        var validationError = ValidateEmployee(updatedEmployee);

        if (validationError != null)
        {
            return BadRequest(validationError);
        }

        var employee = await _context.Employees.FindAsync(id);

        if (employee == null)
        {
            return NotFound("Employee not found.");
        }

        // Check whether another employee already uses this email.
        var emailExists = await _context.Employees
            .AnyAsync(e =>
                e.Id != id &&
                e.Email.ToLower() == updatedEmployee.Email);

        if (emailExists)
        {
            return Conflict(
                "An employee with this email already exists.");
        }

        // Update the existing employee's editable fields.
        employee.FullName = updatedEmployee.FullName;
        employee.Email = updatedEmployee.Email;
        employee.Department = updatedEmployee.Department;
        employee.JobTitle = updatedEmployee.JobTitle;
        employee.IsActive = updatedEmployee.IsActive;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(
                "The employee could not be updated. " +
                "The email may already exist.");
        }

        return NoContent();
    }

    // DELETE: api/employees/5
    // Delete an employee.
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteEmployee(int id)
    {
        var employee = await _context.Employees.FindAsync(id);

        if (employee == null)
        {
            return NotFound("Employee not found.");
        }

        _context.Employees.Remove(employee);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // GET: api/employees/export
    // Download all employees as an Excel workbook.
    [HttpGet("export")]
    public async Task<IActionResult> ExportEmployees()
    {
        var employees = await _context.Employees
            .AsNoTracking()
            .OrderBy(e => e.Id)
            .ToListAsync();

        byte[] excelFile = _excelService.GenerateExcel(employees);

        return File(
            excelFile,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "Employees.xlsx");
    }

    // ==================================================
    // SHARED VALIDATION
    // Used when creating and updating employees.
    // ==================================================
    private static string? ValidateEmployee(Employee employee)
    {
        // Normalize spacing in employee names.
        employee.FullName = Regex.Replace(
            (employee.FullName ?? "").Trim(),
            @"\s+",
            " ");

        if (string.IsNullOrWhiteSpace(employee.FullName))
        {
            return "Full name is required.";
        }

        // Reject names containing numbers or invalid symbols.
        if (!NamePattern.IsMatch(employee.FullName))
        {
            return
                "Enter a valid name using letters. " +
                "Spaces, hyphens and apostrophes are allowed.";
        }

        // Normalize emails consistently.
        employee.Email = (employee.Email ?? "")
            .Trim()
            .ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(employee.Email))
        {
            return "Email address is required.";
        }

        if (!new EmailAddressAttribute().IsValid(employee.Email))
        {
            return "Please enter a valid email address.";
        }

        // Normalize department and job title values.
        employee.Department = (employee.Department ?? "").Trim();
        employee.JobTitle = (employee.JobTitle ?? "").Trim();

        if (string.IsNullOrWhiteSpace(employee.Department))
        {
            return "Please select a department.";
        }

        if (string.IsNullOrWhiteSpace(employee.JobTitle))
        {
            return "Please select a job title.";
        }

        // NEW: Reject departments not in the approved list.
        if (!DepartmentJobs.TryGetValue(
            employee.Department,
            out var allowedJobs))
        {
            return "Please select a valid department.";
        }

        // NEW: Reject unknown job titles and invalid combinations.
        if (!allowedJobs.Contains(
            employee.JobTitle,
            StringComparer.Ordinal))
        {
            return
                "Please select a valid job title " +
                "for the selected department.";
        }

        // Return null when all validations pass.
        return null;
    }
}
