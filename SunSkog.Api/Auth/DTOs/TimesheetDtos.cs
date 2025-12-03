namespace SunSkog.Api.DTOs;

// DTO = Data Transfer Object (co přijímá/vrací API)
public record TimesheetCreateDto(DateOnly PeriodStart, DateOnly PeriodEnd, string? Notes);
public record TimesheetSummaryDto(
    Guid Id,
    string EmployeeId,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    int Status,
    decimal TotalHours,
    decimal TotalKm,
    int TotalPieces,
    decimal TotalPay,
    DateTime? SubmittedAt,
    DateTime? ApprovedAt
);

public record TimesheetDetailDto(
    Guid Id,
    string EmployeeId,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    int Status,
    string? Notes,
    decimal TotalHours,
    decimal TotalKm,
    int TotalPieces,
    decimal TotalPay,
    DateTime? SubmittedAt,
    DateTime? ApprovedAt,
    List<TimesheetEntryDto> Entries
);

public record TimesheetEntryCreateDto(
    DateOnly WorkDate,
    string? Project,
    string? Task,
    decimal Hours,
    decimal Km,
    int Pieces,
    decimal HourRate,
    decimal KmRate,
    decimal PieceRate,
    string? Comment
);

public record TimesheetEntryDto(
    Guid Id,
    DateOnly WorkDate,
    string? Project,
    string? Task,
    decimal Hours,
    decimal Km,
    int Pieces,
    decimal HourRate,
    decimal KmRate,
    decimal PieceRate,
    decimal EntryPay,
    string? Comment
);