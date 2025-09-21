namespace SunSkog.Api.Contracts;

public sealed class AdminTimesheetDetailDto
{
    public Guid Id { get; set; }
    public string? EmployeeEmail { get; set; }
    public string? EmployeeName { get; set; }

    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public int Status { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? Notes { get; set; }

    public decimal TotalHours { get; set; }
    public decimal TotalKm { get; set; }
    public int TotalPieces { get; set; }
    public decimal TotalPay { get; set; }

    public List<AdminTimesheetEntryDto> Entries { get; set; } = new();
}

public sealed class AdminTimesheetEntryDto
{
    public Guid Id { get; set; }
    public DateOnly WorkDate { get; set; }
    public string? Project { get; set; }
    public string? Task { get; set; }
    public decimal Hours { get; set; }
    public decimal Km { get; set; }
    public int Pieces { get; set; }
    public decimal HourRate { get; set; }
    public decimal KmRate { get; set; }
    public decimal PieceRate { get; set; }
    public decimal EntryPay { get; set; }
    public string? Comment { get; set; }
}