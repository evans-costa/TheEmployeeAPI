namespace TheEmployeeAPI.Models;

public class EmployeeBenefits
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public BenefitType BenefitType { get; set; }
    public decimal Cost { get; set; }

    public Employee Employee { get; set; } = null!;
}

public enum BenefitType
{
    Health,
    Dental,
    Vision
}
