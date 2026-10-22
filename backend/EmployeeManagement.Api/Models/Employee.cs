
using System.ComponentModel.DataAnnotations;

namespace EmployeeManagement.Api.Models;

public class Employee
{
    public int Id { get; set; }

    [Required]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Department { get; set; } = string.Empty;

    [Required]
    public string JobTitle { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
