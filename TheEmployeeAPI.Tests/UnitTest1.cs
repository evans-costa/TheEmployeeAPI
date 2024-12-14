using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using TheEmployeeAPI.Data.Repositories;
using TheEmployeeAPI.Models;

namespace TheEmployeeAPI.Tests;

public class BasicTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly int _employeeId;
    private readonly WebApplicationFactory<Program> _factory;

    public BasicTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;

        var repo = _factory.Services.GetRequiredService<IRepository<Employee>>();
        var employee = new Employee
        {
            FirstName = "John",
            LastName = "Smith",
            Address1 = "123 Main Street",
            Benefits =
            [
                new EmployeeBenefits { BenefitType = BenefitType.Health, Cost = 100 },
                new EmployeeBenefits { BenefitType = BenefitType.Dental, Cost = 50 }
            ]
        };
        repo.Create(employee);
        _employeeId = repo.GetAll().First().Id;
    }


    [Fact]
    public async Task GetAllEmployees_ReturnsOkResult()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/employees");

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GetEmployeeById_ReturnsOkResult()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/employees/1");

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GetEmployeeBenefitsById_ReturnsOkResult()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/employees/{_employeeId}/benefits");

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
            FirstName = "John",
            LastName = "Doe"
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
        var response = await client.PutAsJsonAsync("/employees/1", new Employee
        {
            FirstName = "John",
            LastName = "Doe",
            Address1 = "123 Main St"
        });

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task UpdateEmployee_ReturnsBadRequestWhenAddressIsNull()
    {
        var client = _factory.CreateClient();
        var invalidEmployee = new UpdateEmployeeRequest();

        var response = await client.PutAsJsonAsync($"/employees/{_employeeId}", invalidEmployee);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problemDetails);
        Assert.Contains("Address1", problemDetails.Errors.Keys);
    }
}
