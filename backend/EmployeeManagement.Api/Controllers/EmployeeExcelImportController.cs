
using EmployeeManagement.Api.Data;
using EmployeeManagement.Api.Models;
using EmployeeManagement.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagement.Api.Controllers;

// Handles importing employee records from Excel workbooks.
[ApiController]
[Route("api/employees")]
public class EmployeeExcelImportController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly EmployeeExcelImportService _importService;

    // Dependencies are provided through dependency injection.
    public EmployeeExcelImportController(
        AppDbContext context,
        EmployeeExcelImportService importService)
    {
        _context = context;
        _importService = importService;
    }

    // POST: api/employees/import
    // Adds new employees and skips employees already in SQL Server.
    [HttpPost("import")]
    public async Task<IActionResult> ImportEmployees(
        [FromForm] IFormFile file)
    {
        // Check that a file was selected.
        if (file == null || file.Length == 0)
        {
            return BadRequest("Please select an Excel file.");
        }

        // Accept only Excel .xlsx files.
        if (!Path.GetExtension(file.FileName)
            .Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Only .xlsx Excel files are supported.");
        }

        // Restrict uploads to a maximum of 5 MB.
        if (file.Length > 5 * 1024 * 1024)
        {
            return BadRequest("The Excel file must be 5 MB or smaller.");
        }

        try
        {
            // Read employees from the uploaded Excel workbook.
            await using var stream = file.OpenReadStream();

            var excelEmployees = _importService.ReadEmployees(stream);

            // Check that the workbook contains employee records.
            if (excelEmployees.Count == 0)
            {
                return BadRequest(
                    "The Excel file contains no employee records.");
            }

            // Retrieve existing employee emails from SQL Server.
            // AsNoTracking avoids tracking read-only data.
            var existingEmails = await _context.Employees
                .AsNoTracking()
                .Select(employee => employee.Email)
                .ToListAsync();

            // Store existing emails for efficient duplicate checking.
            // Email comparisons are case-insensitive.
            var existingEmailSet = new HashSet<string>(
                existingEmails.Select(email => email.Trim()),
                StringComparer.OrdinalIgnoreCase
            );

            // Track new emails found within the uploaded Excel file.
            var newEmailSet = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase
            );

            // Store only employees who are not already registered.
            var newEmployees = new List<Employee>();

            // Check every employee in the workbook.
            foreach (var employee in excelEmployees)
            {
                // Normalize email addresses before comparing.
                employee.Email =
                    employee.Email.Trim().ToLowerInvariant();

                // Skip employees who are already in SQL Server.
                if (existingEmailSet.Contains(employee.Email))
                {
                    continue;
                }

                // Skip repeated new emails within the workbook.
                if (!newEmailSet.Add(employee.Email))
                {
                    continue;
                }

                // Add this new employee to the import list.
                newEmployees.Add(employee);
            }

            // If all employees already exist, don't insert anything.
            if (newEmployees.Count == 0)
            {
                return Ok(new
                {
                    message = "No new employees to add.",
                    importedCount = 0
                });
            }

            // Add only new employee records to SQL Server.
            _context.Employees.AddRange(newEmployees);

            // Save new employee records permanently.
            await _context.SaveChangesAsync();

            // Show only the number of employees added.
            return Ok(new
            {
                message =
                    $"{newEmployees.Count} new employees added successfully.",
                importedCount = newEmployees.Count
            });
        }
        catch (ArgumentException ex)
        {
            // Handle invalid or incomplete employee information.
            return BadRequest(ex.Message);
        }
        catch (System.IO.InvalidDataException)
        {
            // Handle invalid workbook data.
            return BadRequest(
                "The Excel workbook is invalid or cannot be read.");
        }
        catch (DbUpdateException)
        {
            // Handle database errors such as duplicate email conflicts.
            return Conflict(
                "The employees could not be saved. " +
                "Please check the employee data and try again.");
        }
    }
}
