using System.ComponentModel.DataAnnotations;

namespace SunSkog.Api.Storage.Entities;

public enum TimesheetStatus
{
    Draft = 0,
    Submitted = 1,
    Approved = 2,
    Returned = 3
}

public class Timesheet
{
    [Key] public Guid Id { get; set; }

    // FK na AspNetUsers.Id (string)
    [Required] public string EmployeeId { get; set; } = default!;

    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd   { get; set; }

    public TimesheetStatus Status { get; set; } = TimesheetStatus.Draft;

    public string? Notes { get; set; }

    public decimal TotalHours  { get; set; }
    public decimal TotalKm     { get; set; }
    public int     TotalPieces { get; set; }
    public decimal TotalPay    { get; set; }

    public DateTime? SubmittedAt { get; set; }
    public DateTime? ApprovedAt  { get; set; }

    public List<TimesheetEntry> Entries { get; set; } = new();
}