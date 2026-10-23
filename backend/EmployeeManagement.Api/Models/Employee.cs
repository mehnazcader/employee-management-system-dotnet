//imports [Required] and [EmailAddress] validation attributes.
using System.ComponentModel.DataAnnotations;

// Groups this class in the project's Models namespace
//Employee class is inside the Models folder, and it belongs to the EmployeeManagement.Api.Models namespace.
namespace EmployeeManagement.Api.Models;

public class Employee
{
    // Primary key by EF Core naming convention.
    // SQL Server generates the ID automatically
    public int Id { get; set; }

    // Requires a non-empty value during model validation.
    [Required]
    // 'get' reads the value; 'set' changes it.
    // string.Empty initializes with "" instead of null
    public string FullName { get; set; } = string.Empty;

    // Email is required and checked for valid-looking format.
    // This does NOT check whether the email is unique
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    // Stores the department, e.g., IT
    [Required]
    public string Department { get; set; } = string.Empty;

    // Stores the employee's job title
    [Required]
    public string JobTitle { get; set; } = string.Empty;

    // A new Employee object has IsActive set to true by default.
    //This means that when a new employee is added to the system, they are considered active unless explicitly set otherwise.
    public bool IsActive { get; set; } = true;
}
