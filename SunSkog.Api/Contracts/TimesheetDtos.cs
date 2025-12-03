using System;
using System.Collections.Generic;

namespace SunSkog.Api.Contracts
{
    // ----- VstupnĂ­ DTO pro poloĹľku vĂ˝kazu (pĹ™i vytvĂˇĹ™enĂ­) -----
    public class EntryDto
    {
        public DateOnly WorkDate { get; set; }
        public string Project { get; set; } = "";
        public string Task { get; set; } = "";
        public decimal Hours { get; set; }
        public decimal Km { get; set; }
        public int Pieces { get; set; }
        public decimal HourRate { get; set; }
        public decimal KmRate { get; set; }
        public decimal PieceRate { get; set; }
        public string? Comment { get; set; }
    }

    // ----- VstupnĂ­ DTO pro vytvoĹ™enĂ­ vĂ˝kazu -----
    public class CreateDto
    {
        public DateOnly PeriodStart { get; set; }
        public DateOnly PeriodEnd { get; set; }
        public List<EntryDto>? Entries { get; set; }
    }

    // ----- VstupnĂ­ DTO s poznĂˇmkou (approve/return) -----
    public class NoteDto
    {
        public string? Notes { get; set; }
    }

    // ----- VĂ˝stup pro seznam vĂ˝kazĹŻ (admin i /my) -----
    public record TimesheetListItemDto(
        Guid Id,
        string EmployeeId,
        DateOnly PeriodStart,
        DateOnly PeriodEnd,
        int Status,
        DateTime? SubmittedAt,
        DateTime? ApprovedAt,
        string? Notes,
        decimal TotalHours,
        decimal TotalKm,
        int TotalPieces,
        decimal TotalPay
    );

    // ----- VĂ˝stupnĂ­ detail poloĹľky vĂ˝kazu -----
    public record TimesheetEntryDetailDto(
        Guid Id,
        DateOnly WorkDate,
        string Project,
        string Task,
        decimal Hours,
        decimal Km,
        int Pieces,
        decimal HourRate,
        decimal KmRate,
        decimal PieceRate,
        decimal EntryPay,
        string? Comment
    );

    // ----- VĂ˝stupnĂ­ detail vĂ˝kazu -----
    public record TimesheetDetailDto(
        Guid Id,
        string EmployeeId,
        DateOnly PeriodStart,
        DateOnly PeriodEnd,
        int Status,
        DateTime? SubmittedAt,
        DateTime? ApprovedAt,
        string? Notes,
        decimal TotalHours,
        decimal TotalKm,
        int TotalPieces,
        decimal TotalPay,
        List<TimesheetEntryDetailDto> Entries
    );

    public record AddEntryDto(
        DateOnly WorkDate,
        string   Project,
        string   Task,
        decimal  Hours,
        decimal  Km,
        int      Pieces,
        decimal  HourRate,
        decimal  KmRate,
        decimal  PieceRate,
        string?  Comment
    );

    public record UpdateEntryDto(
        DateOnly? WorkDate,
        string?   Project,
        string?   Task,
        decimal?  Hours,
        decimal?  Km,
        int?      Pieces,
        decimal?  HourRate,
        decimal?  KmRate,
        decimal?  PieceRate,
        string?   Comment
    );
}
