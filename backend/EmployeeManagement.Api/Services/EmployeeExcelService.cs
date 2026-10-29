
using ClosedXML.Excel;
using EmployeeManagement.Api.Models;

namespace EmployeeManagement.Api.Services;

// Responsible for generating Excel files containing employee information.
public class EmployeeExcelService : IEmployeeExcelService
{
    public byte[] GenerateExcel(IEnumerable<Employee> employees)
    {
        // Create a new Excel workbook.
        using var workbook = new XLWorkbook();

        // Add a worksheet named Employees.
        var worksheet = workbook.Worksheets.Add("Employees");

        // Create the column headers.
        string[] headers =
        {
            "ID",
            "Full Name",
            "Email",
            "Department",
            "Job Title",
            "Status"
        };

        // Write the column headers into the first row.
        for (int column = 0; column < headers.Length; column++)
        {
            worksheet.Cell(1, column + 1).Value = headers[column];
        }

        // Format the header row.
        var headerRange = worksheet.Range(1, 1, 1, headers.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.DarkGreen;
        headerRange.Style.Font.FontColor = XLColor.White;

        // Add each employee to a new row.
        int row = 2;

        foreach (var employee in employees)
        {
            worksheet.Cell(row, 1).Value = employee.Id;
            worksheet.Cell(row, 2).Value = employee.FullName;
            worksheet.Cell(row, 3).Value = employee.Email;
            worksheet.Cell(row, 4).Value = employee.Department;
            worksheet.Cell(row, 5).Value = employee.JobTitle;
            worksheet.Cell(row, 6).Value =
                employee.IsActive ? "Active" : "Inactive";

            row++;
        }

        // Adjust column widths to fit the content.
        worksheet.Columns().AdjustToContents();

        // Save the Excel workbook into memory.
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        // Return the Excel file as bytes.
        return stream.ToArray();
    }
}
