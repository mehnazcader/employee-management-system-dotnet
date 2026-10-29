using EmployeeManagement.Api.Models;

namespace EmployeeManagement.Api.Services;

// Defines the contract for generating employee Excel files.
public interface IEmployeeExcelService
{
    // Generate an Excel workbook containing employee records.
    byte[] GenerateExcel(IEnumerable<Employee> employees);
}