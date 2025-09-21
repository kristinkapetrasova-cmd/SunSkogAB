namespace SunSkog.Api.Contracts;

public sealed class AdminEmployeeSummaryDto
{
    public string? EmployeeEmail { get; set; }
    public string? EmployeeName  { get; set; }

    public int TotalTimesheets   { get; set; }
    public int DraftCount        { get; set; }
    public int SubmittedCount    { get; set; }
    public int ApprovedCount     { get; set; }
    public int ReturnedCount     { get; set; }

    public decimal TotalHours    { get; set; }
    public decimal TotalKm       { get; set; }
    public int     TotalPieces   { get; set; }
    public decimal TotalPay      { get; set; }
}

public sealed class AdminDailyTotalsDto
{
    public DateOnly Date         { get; set; }
    public decimal  TotalHours   { get; set; }
    public decimal  TotalKm      { get; set; }
    public int      TotalPieces  { get; set; }
    public decimal  TotalPay     { get; set; }
}