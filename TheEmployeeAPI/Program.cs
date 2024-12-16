using System.Globalization;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TheEmployeeAPI;
using TheEmployeeAPI.Data.Repositories;
using TheEmployeeAPI.Infrastructure;
using TheEmployeeAPI.Models;

var defaultCulture = new CultureInfo("en-US");
CultureInfo.DefaultThreadCurrentCulture = defaultCulture;
CultureInfo.DefaultThreadCurrentUICulture = defaultCulture;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "TheEmployeeAPI.xml"));
});
builder.Services.AddSingleton<IRepository<Employee>, EmployeeRepository>();
builder.Services.AddProblemDetails();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers(options => { options.Filters.Add<FluentValidationFilter>(); });
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlite("Data Source=./Infrastructure/Database/employee.db");
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.UseHttpsRedirection();

app.Run();

public partial class Program
{
}
