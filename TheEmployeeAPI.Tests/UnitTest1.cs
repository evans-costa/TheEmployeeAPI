using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using TheEmployeeAPI.Abstractions;
using TheEmployeeAPI.Infrastructure;
using TheEmployeeAPI.Models;

namespace TheEmployeeAPI.Tests;

public class BasicTests : IClassFixture<CustomWebApplicationFactory>
{
    private const int EmployeeId = 1;
    private readonly CustomWebApplicationFactory _factory;

    public BasicTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }


    [Fact]
    public async Task GetAllEmployees_ReturnsOkResult()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/employees");

        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to get employees: {content}");
        }

        var employees =
            await response.Content.ReadFromJsonAsync<IEnumerable<GetEmployeeResponse>>();
        Assert.NotEmpty(employees!);
    }

    [Fact]
    public async Task GetAllEmployees_WithFilter_ReturnsOneResult()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/employees?FirstNameContains=John");

        response.EnsureSuccessStatusCode();

        var employees =
            await response.Content.ReadFromJsonAsync<IEnumerable<GetEmployeeResponse>>();
        Assert.Single(employees!);
    }

    [Fact]
    public async Task GetEmployeeById_ReturnsOkResult()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/employees/{EmployeeId}");

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GetEmployeeBenefitsById_ReturnsOkResult()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/employees/{EmployeeId}/benefits");

        response.EnsureSuccessStatusCode();

        var benefits = await response.Content
            .ReadFromJsonAsync<IEnumerable<GetEmployeeResponseEmployeeBenefit>>();
        Assert.Equal(2, benefits!.Count());
    }

    [Fact]
    public async Task CreateEmployee_ReturnsOkResult()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/employees", new Employee
        {
            FirstName = "Peter",
            LastName = "Moe"
        });

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task CreateEmployee_ReturnsBadRequestResult()
    {
        var client = _factory.CreateClient();
        var invalidEmployee = new CreateEmployeeRequest();

        var response = await client.PostAsJsonAsync("/employees", invalidEmployee);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.NotNull(problemDetails);

        Assert.Contains("FirstName", problemDetails.Errors.Keys);
        Assert.Contains("LastName", problemDetails.Errors.Keys);
        Assert.Contains("'First Name' must not be empty.", problemDetails.Errors["FirstName"]);
        Assert.Contains("'Last Name' must not be empty.", problemDetails.Errors["LastName"]);
    }

    [Fact]
    public async Task UpdateEmployee_ReturnsOkResult()
    {
        var client = _factory.CreateClient();
        var response = await client.PutAsJsonAsync($"/employees/{EmployeeId}", new Employee
        {
            FirstName = "John",
            LastName = "Doe",
            Address1 = "123 Main St"
        });

        response.EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var employee = await db.Employees.FindAsync(EmployeeId);
        Assert.Equal("123 Main St", employee?.Address1);
    }

    [Fact]
    public async Task UpdateEmployee_ReturnsBadRequestWhenAddressIsNull()
    {
        var client = _factory.CreateClient();
        var invalidEmployee = new UpdateEmployeeRequest();

        var response = await client.PutAsJsonAsync($"/employees/{EmployeeId}", invalidEmployee);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problemDetails);
        Assert.Contains("Address1", problemDetails.Errors.Keys);
    }

    [Fact]
    public async Task DeleteEmployee_ReturnsNoContentResult()
    {
        var client = _factory.CreateClient();
        var newEmployee = new Employee
        {
            FirstName = "Paul",
            LastName = "Doe"
        };

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Employees.Add(newEmployee);
        await db.SaveChangesAsync();

        var response = await client.DeleteAsync($"/employees/{newEmployee.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteEmployee_ReturnsNotFoundResult()
    {
        var client = _factory.CreateClient();
        var response = await client.DeleteAsync("/employees/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}