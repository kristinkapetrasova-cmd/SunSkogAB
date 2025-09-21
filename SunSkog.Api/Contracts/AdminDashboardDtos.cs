namespace SunSkog.Api.Contracts;

public sealed class AdminDashboardDto
{
    public StatusCountsDto StatusCounts { get; set; } = new();
    public TotalsDto Totals { get; set; } = new();
    public List<RecentItemDto> Recent { get; set; } = new();
    public List<TopEmployeeDto> TopEmployees { get; set; } = new();
}

public sealed class StatusCountsDto
{
    public int Draft { get; set; }
    public int Submitted { get; set; }
    public int Approved { get; set; }
    public int Returned { get; set; }
}

public sealed class TotalsDto
{
    public decimal TotalHours { get; set; }
    public decimal TotalKm { get; set; }
    public int TotalPieces { get; set; }
    public decimal TotalPay { get; set; }
}

public sealed class RecentItemDto
{
    public Guid Id { get; set; }
    public string? EmployeeEmail { get; set; }
    public string? EmployeeName { get; set; }
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public int Status { get; set; }
    public DateTime? LastChange { get; set; }
}

public sealed class TopEmployeeDto
{
    public string? EmployeeEmail { get; set; }
    public string? EmployeeName { get; set; }
    public decimal TotalPay { get; set; }
    public decimal TotalHours { get; set; }
}