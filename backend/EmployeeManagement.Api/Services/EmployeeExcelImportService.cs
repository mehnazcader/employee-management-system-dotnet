
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using EmployeeManagement.Api.Models;

namespace EmployeeManagement.Api.Services;

// Reads and validates new employee records from Excel.
// Database operations remain the controller's responsibility.
public class EmployeeExcelImportService
{
    // Accept names containing letters, accents, spaces,
    // apostrophes, hyphens and periods.
    private static readonly Regex NamePattern = new(
        @"^\p{L}[\p{L}\p{M}]*(?:[ .'-]\p{L}[\p{L}\p{M}]*)*$",
        RegexOptions.Compiled,
        TimeSpan.FromSeconds(1));

    // Approved department and job title combinations.
    // These match the dropdown options in App.jsx.
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

    // Reads the workbook and returns validated employee objects.
    // SQL Server will generate IDs when these records are saved.
    public List<Employee> ReadEmployees(Stream excelStream)
    {
        using var workbook = new XLWorkbook(excelStream);

        if (workbook.Worksheets.Count == 0)
        {
            throw new ArgumentException(
                "The Excel workbook does not contain a worksheet.");
        }

        var worksheet = workbook.Worksheet(1);

        // The file must follow the same six-column structure
        // as the employee export.
        string[] expectedHeaders =
        {
            "ID",
            "Full Name",
            "Email",
            "Department",
            "Job Title",
            "Status"
        };

        for (int column = 1; column <= expectedHeaders.Length; column++)
        {
            var actualHeader = worksheet.Cell(1, column)
                .GetString()
                .Trim();

            if (!actualHeader.Equals(
                expectedHeaders[column - 1],
                StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    $"Invalid Excel template. Column {column} " +
                    $"must be '{expectedHeaders[column - 1]}'.");
            }
        }

        var employees = new List<Employee>();

        // Start at row 2 because row 1 contains headings.
        foreach (var row in worksheet.RowsUsed().Skip(1))
        {
            // Ignore completely empty rows.
            if (row.Cells(1, 6).All(cell => cell.IsEmpty()))
            {
                continue;
            }

            // ID is intentionally ignored.
            // The database generates IDs for newly imported employees.
            var fullName = Regex.Replace(
                row.Cell(2).GetString().Trim(),
                @"\s+",
                " ");

            var email = row.Cell(3)
                .GetString()
                .Trim()
                .ToLowerInvariant();

            var department = row.Cell(4).GetString().Trim();
            var jobTitle = row.Cell(5).GetString().Trim();
            var status = row.Cell(6).GetString().Trim();

            int rowNumber = row.RowNumber();

            // Validate the employee's name.
            if (string.IsNullOrWhiteSpace(fullName))
            {
                throw new ArgumentException(
                    $"Row {rowNumber}: Full name is required.");
            }

            if (!NamePattern.IsMatch(fullName))
            {
                throw new ArgumentException(
                    $"Row {rowNumber}: Enter a valid name using letters. " +
                    "Spaces, hyphens and apostrophes are allowed.");
            }

            // Validate the email.
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException(
                    $"Row {rowNumber}: Email address is required.");
            }

            if (!new EmailAddressAttribute().IsValid(email))
            {
                throw new ArgumentException(
                    $"Row {rowNumber}: Please enter a valid email address.");
            }

            // Validate department and job title.
            if (string.IsNullOrWhiteSpace(department))
            {
                throw new ArgumentException(
                    $"Row {rowNumber}: Department is required.");
            }

            if (string.IsNullOrWhiteSpace(jobTitle))
            {
                throw new ArgumentException(
                    $"Row {rowNumber}: Job title is required.");
            }

            if (!DepartmentJobs.TryGetValue(
                department,
                out var allowedJobs))
            {
                throw new ArgumentException(
                    $"Row {rowNumber}: Unknown department '{department}'.");
            }

            if (!allowedJobs.Contains(
                jobTitle,
                StringComparer.Ordinal))
            {
                throw new ArgumentException(
                    $"Row {rowNumber}: Job title '{jobTitle}' " +
                    $"is not valid for department '{department}'.");
            }

            // Status must be Active or Inactive.
            if (!status.Equals(
                    "Active",
                    StringComparison.OrdinalIgnoreCase) &&
                !status.Equals(
                    "Inactive",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    $"Row {rowNumber}: Status must be Active or Inactive.");
            }

            // All fields passed validation.
            employees.Add(new Employee
            {
                FullName = fullName,
                Email = email,
                Department = department,
                JobTitle = jobTitle,
                IsActive = status.Equals(
                    "Active",
                    StringComparison.OrdinalIgnoreCase)
            });
        }

        return employees;
    }
}
