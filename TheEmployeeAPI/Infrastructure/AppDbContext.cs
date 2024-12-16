using Microsoft.EntityFrameworkCore;
using TheEmployeeAPI.Models;

namespace TheEmployeeAPI.Infrastructure;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Employee> Employees { get; set; }
}
