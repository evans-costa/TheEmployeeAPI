using FluentValidation;
using TheEmployeeAPI.Infrastructure;

namespace TheEmployeeAPI.Abstractions;

public class UpdateEmployeeRequest
{
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
}

public class UpdateEmployeeRequestValidator : AbstractValidator<UpdateEmployeeRequest>
{
    private readonly AppDbContext _dbContext;
    private readonly HttpContext _httpContext;

    public UpdateEmployeeRequestValidator(IHttpContextAccessor httpContextAccessor,
        AppDbContext dbContext)
    {
        _dbContext = dbContext;
        _httpContext = httpContextAccessor.HttpContext!;

        RuleFor(x => x.Address1)
            .MustAsync(NotBeEmptyIfItIsSetOnEmployeeAlreadyAsync)
            .WithMessage("Address1 must not be empty");
    }

    private async Task<bool> NotBeEmptyIfItIsSetOnEmployeeAlreadyAsync(string? address,
        CancellationToken token)
    {
        var id = Convert.ToInt32(_httpContext.Request.RouteValues["id"]);
        var employee = await _dbContext.Employees.FindAsync([id], token);

        return employee!.Address1 == null || !string.IsNullOrWhiteSpace(address);
    }
}
