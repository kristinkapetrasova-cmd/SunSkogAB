using System;
using System.Collections.Generic;

namespace SunSkog.Api.Models.Domain
{
    public enum TimesheetStatus
    {
        Draft = 0,
        Submitted = 1,
        Approved = 2,
        Returned = 3
    }

    public class Timesheet
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        // vazba na uživatele (Identity)
        public string? EmployeeId { get; set; }

        public DateOnly PeriodStart { get; set; }
        public DateOnly PeriodEnd { get; set; }
        public TimesheetStatus Status { get; set; } = TimesheetStatus.Draft;

        public DateTime? SubmittedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? Notes { get; set; }

        public decimal TotalHours { get; set; }
        public decimal TotalKm { get; set; }
        public int TotalPieces { get; set; }
        public decimal TotalPay { get; set; }

        public ICollection<TimesheetEntry> Entries { get; set; } = new List<TimesheetEntry>();
    }

    public class TimesheetEntry
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        // FK MUSÍ být Guid a jmenovat se stejně, jak mapujeme v DbContextu
        public Guid TimesheetId { get; set; }
        public Timesheet? Timesheet { get; set; }

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
}