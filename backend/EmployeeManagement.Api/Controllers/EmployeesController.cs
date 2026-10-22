
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

    // Constructor injection: ASP.NET Core provides AppDbContext.
    public EmployeesController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/employees
    // Retrieve all employees from SQL Server.
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Employee>>> GetEmployees()
    {
        var employees = await _context.Employees.ToListAsync();
        return Ok(employees);
    }

    // POST: api/employees
    // Add a new employee to SQL Server.
    [HttpPost]
    public async Task<ActionResult<Employee>> CreateEmployee(Employee employee)
    {
        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();

        return Created(
            $"/api/employees/{employee.Id}",
            employee
        );
    }
}
