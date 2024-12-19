using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TheEmployeeAPI.Abstractions;
using TheEmployeeAPI.Infrastructure;
using TheEmployeeAPI.Models;

namespace TheEmployeeAPI.Controllers;

public class EmployeesController : BaseController
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<EmployeesController> _logger;

    public EmployeesController(ILogger<EmployeesController> logger, AppDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    /// <summary>
    ///     Gets all the employees in the system
    /// </summary>
    /// <returns>An array of all employees.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<GetEmployeeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllEmployees([FromQuery] GetAllEmployeesRequest? request)
    {
        var page = request?.Page ?? 1;
        var numberOfRecords = request?.RecordsPerPage ?? 100;

        var query = _dbContext.Employees
            .Include(e => e.Benefits)
            .Skip((page - 1) * numberOfRecords)
            .Take(numberOfRecords);

        if (request is not null)
        {
            if (!string.IsNullOrWhiteSpace(request.FirstNameContains))
                query = query.Where(e => e.FirstName.Contains(request.FirstNameContains));

            if (!string.IsNullOrWhiteSpace(request.LastNameContains))
                query = query.Where(e => e.LastName.Contains(request.LastNameContains));
        }

        var employees = await query
            .Select(employee => new GetEmployeeResponse
            {
                FirstName = employee.FirstName,
                LastName = employee.LastName,
                Address1 = employee.Address1,
                Address2 = employee.Address2,
                City = employee.City,
                State = employee.State,
                ZipCode = employee.ZipCode,
                PhoneNumber = employee.PhoneNumber,
                Email = employee.Email,
                Benefits = employee.Benefits.Select(benefit =>
                    new GetEmployeeResponseEmployeeBenefit
                    {
                        Id = benefit.Id,
                        EmployeeId = benefit.EmployeeId,
                        BenefitType = benefit.BenefitType,
                        Cost = benefit.Cost
                    }).ToList()
            })
            .ToArrayAsync();

        return Ok(employees);
    }

    /// <summary>
    ///     Gets an employee by ID.
    /// </summary>
    /// <param name="id">The ID of the employee.</param>
    /// <returns>The single employee record.</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(GetEmployeeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetEmployeeById(int id)
    {
        var employee = await _dbContext.Employees.SingleOrDefaultAsync(e => e.Id == id);

        if (employee == null) return NotFound();

        var employeeResponse = EmployeeToGetEmployeeResponse(employee);

        return Ok(employeeResponse);
    }

    /// <summary>
    ///     Gets the benefits for an employee.
    /// </summary>
    /// <param name="id">The ID to get the benefits for.</param>
    /// <returns>The benefits for that employee.</returns>
    [HttpGet("{id:int}/benefits")]
    [ProducesResponseType(typeof(IEnumerable<GetEmployeeResponseEmployeeBenefit>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetEmployeeBenefitsByEmployeeId(int id)
    {
        var employee = await _dbContext.Employees
            .Include(employee => employee.Benefits)
            .SingleOrDefaultAsync(e => e.Id == id);

        if (employee == null) return NotFound();

        return Ok(employee.Benefits.Select(BenefitToBenefitResponse));
    }

    /// <summary>
    ///     Creates a new employee.
    /// </summary>
    /// <param name="employeeRequest">The employee to be created.</param>
    /// <returns>A link to the employee that was created.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(GetEmployeeResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeRequest
        employeeRequest)
    {
        var newEmployee = new Employee
        {
            FirstName = employeeRequest.FirstName!,
            LastName = employeeRequest.LastName!,
            SocialSecurityNumber = employeeRequest.SocialSecurityNumber,
            Address1 = employeeRequest.Address1,
            Address2 = employeeRequest.Address2,
            City = employeeRequest.City,
            State = employeeRequest.State,
            ZipCode = employeeRequest.ZipCode,
            PhoneNumber = employeeRequest.PhoneNumber,
            Email = employeeRequest.Email
        };

        _dbContext.Employees.Add(newEmployee);
        await _dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(GetEmployeeById), new { newEmployee.Id }, newEmployee);
    }

    /// <summary>
    ///     Updates an employee.
    /// </summary>
    /// <param name="id">The ID of the employee to update.</param>
    /// <param name="employeeRequest">The employee data to update.</param>
    /// <returns></returns>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(GetEmployeeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateEmployee(int id,
        [FromBody] UpdateEmployeeRequest
            employeeRequest)
    {
        var existingEmployee = await _dbContext.Employees
            .AsTracking()
            .SingleOrDefaultAsync(e => e.Id == id);

        if (existingEmployee == null) return NotFound();

        existingEmployee.Address1 = employeeRequest.Address1;
        existingEmployee.Address2 = employeeRequest.Address2;
        existingEmployee.City = employeeRequest.City;
        existingEmployee.State = employeeRequest.State;
        existingEmployee.ZipCode = employeeRequest.ZipCode;
        existingEmployee.PhoneNumber = employeeRequest.PhoneNumber;
        existingEmployee.Email = employeeRequest.Email;

        await _dbContext.SaveChangesAsync();
        return Ok(existingEmployee);
    }

    /// <summary>
    ///     Deletes an employee.
    /// </summary>
    /// <param name="id">The ID of the employee to delete.</param>
    /// <returns></returns>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteEmployee(int id)
    {
        var employee = await _dbContext.Employees.SingleOrDefaultAsync(e => e.Id == id);

        if (employee == null) return NotFound();

        _dbContext.Employees.Remove(employee);
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

    private static GetEmployeeResponse EmployeeToGetEmployeeResponse(Employee employee)
    {
        return new GetEmployeeResponse
        {
            FirstName = employee.FirstName,
            LastName = employee.LastName,
            Address1 = employee.Address1,
            Address2 = employee.Address2,
            City = employee.City,
            State = employee.State,
            ZipCode = employee.ZipCode,
            PhoneNumber = employee.PhoneNumber,
            Email = employee.Email,
            Benefits = employee.Benefits.Select(BenefitToBenefitResponse).ToList()
        };
    }

    private static GetEmployeeResponseEmployeeBenefit BenefitToBenefitResponse(EmployeeBenefits
        benefit)
    {
        return new GetEmployeeResponseEmployeeBenefit
        {
            Id = benefit.Id,
            EmployeeId = benefit.EmployeeId,
            BenefitType = benefit.BenefitType,
            Cost = benefit.Cost
        };
    }
}
