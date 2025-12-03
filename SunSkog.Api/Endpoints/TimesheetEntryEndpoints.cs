using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SunSkog.Api.Data;
using SunSkog.Api.Storage.Entities;

namespace SunSkog.Api.Endpoints;

public static class TimesheetEntryEndpoints
{
    public static IEndpointRouteBuilder MapTimesheetEntryEndpoints(this IEndpointRouteBuilder app)
    {
        var grp = app.MapGroup("/api/timesheets/{timesheetId:guid}/entries")
                     .WithTags("Timesheet Entries")
                     .RequireAuthorization();

        // GET: list položek daného timesheetu
        grp.MapGet("", async (Guid timesheetId, ApplicationDbContext db) =>
        {
            var ts = await db.Timesheets
                .Include(t => t.Entries)
                .AsNoTracking()
                .SingleOrDefaultAsync(t => t.Id == timesheetId);

            if (ts is null)
                return Results.NotFound(new { error = "Timesheet nenalezen." });

            var dto = new TimesheetDetailDto(
                ts.Id,
                ts.EmployeeId,
                ts.PeriodStart,
                ts.PeriodEnd,
                ts.Status.ToString(),
                ts.Notes,
                ts.TotalHours,
                ts.TotalKm,
                ts.TotalPieces,
                ts.TotalPay,
                ts.SubmittedAt,
                ts.ApprovedAt,
                ts.Entries
                    .OrderBy(e => e.WorkDate)
                    .Select(ToDto)
                    .ToList()
            );

            return Results.Ok(dto);
        })
        .Produces<TimesheetDetailDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .WithOpenApi();

        // POST: vytvořit položku
        grp.MapPost("", async (Guid timesheetId, ApplicationDbContext db, CreateEntryRequest req) =>
        {
            var ts = await db.Timesheets
                .Include(t => t.Entries)
                .SingleOrDefaultAsync(t => t.Id == timesheetId);

            if (ts is null)
                return Results.NotFound(new { error = "Timesheet nenalezen." });

            // Kontrola: nelze přidávat položky do schváleného nebo odeslaného výkazu
            if (ts.Status == TimesheetStatus.Approved)
                return Results.Problem(statusCode: 403, title: "Forbidden", detail: "Schválený výkaz nelze upravovat.");
            
            if (ts.Status == TimesheetStatus.Submitted)
                return Results.Problem(statusCode: 403, title: "Forbidden", detail: "Odeslaný výkaz nelze upravovat. Nejprve ho vraťte k přepracování.");

            // Validace: datum záznamu musí být v období výkazu
            if (req.WorkDate < ts.PeriodStart || req.WorkDate > ts.PeriodEnd)
                return Results.BadRequest(new { error = "Datum záznamu musí být v období výkazu." });

            var entry = new TimesheetEntry
            {
                Id = Guid.NewGuid(),
                TimesheetId = ts.Id,
                WorkDate = req.WorkDate,
                Project = req.Project,
                Task = req.Task,
                Hours = req.Hours,
                Km = req.Km,
                Pieces = req.Pieces,
                HourRate = req.HourRate,
                KmRate = req.KmRate,
                PieceRate = req.PieceRate,
                Comment = req.Comment,

                // rozšířená pole (volitelná)
                FromTime = req.FromTime,
                ToTime = req.ToTime,
                PauseMinutes = req.PauseMinutes,
                TravelMinutes = req.TravelMinutes,
                AreaCode = req.AreaCode,
                AreaName = req.AreaName,
                TrKind = req.TrKind,
                Hectares = req.Hectares,
                HectareRate = req.HectareRate,
                HectarePay = 0m, // spočítáme níže
                BoxCarryCount = req.BoxCarryCount,
                ExtraNote = req.ExtraNote
            };

            entry.EntryPay = CalculateEntryPay(entry);

            db.TimesheetEntries.Add(entry);
            await db.SaveChangesAsync();

            await RecalculateTimesheetTotals(db, ts.Id);

            return Results.Created($"/api/timesheets/{timesheetId}/entries/{entry.Id}", ToDto(entry));
        })
        .Produces<TimesheetEntryDto>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status404NotFound)
        .WithOpenApi();

        // PUT: upravit položku
        grp.MapPut("/{entryId:guid}", async (Guid timesheetId, Guid entryId, ApplicationDbContext db, UpdateEntryRequest req) =>
        {
            var entry = await db.TimesheetEntries.SingleOrDefaultAsync(e => e.Id == entryId && e.TimesheetId == timesheetId);
            if (entry is null)
                return Results.NotFound(new { error = "Položka nenalezena." });

            // Načíst timesheet pro validaci období a statusu
            var ts = await db.Timesheets.FindAsync(timesheetId);
            if (ts is null)
                return Results.NotFound(new { error = "Timesheet nenalezen." });

            // Kontrola: nelze upravovat položky ve schváleném nebo odeslaném výkazu
            if (ts.Status == TimesheetStatus.Approved)
                return Results.Problem(statusCode: 403, title: "Forbidden", detail: "Schválený výkaz nelze upravovat.");
            
            if (ts.Status == TimesheetStatus.Submitted)
                return Results.Problem(statusCode: 403, title: "Forbidden", detail: "Odeslaný výkaz nelze upravovat. Nejprve ho vraťte k přepracování.");

            // Validace období
            if (req.WorkDate < ts.PeriodStart || req.WorkDate > ts.PeriodEnd)
                return Results.BadRequest(new { error = "Datum záznamu musí být v období výkazu." });

            entry.WorkDate = req.WorkDate;
            entry.Project = req.Project;
            entry.Task = req.Task;
            entry.Hours = req.Hours;
            entry.Km = req.Km;
            entry.Pieces = req.Pieces;
            entry.HourRate = req.HourRate;
            entry.KmRate = req.KmRate;
            entry.PieceRate = req.PieceRate;
            entry.Comment = req.Comment;

            entry.FromTime = req.FromTime;
            entry.ToTime = req.ToTime;
            entry.PauseMinutes = req.PauseMinutes;
            entry.TravelMinutes = req.TravelMinutes;
            entry.AreaCode = req.AreaCode;
            entry.AreaName = req.AreaName;
            entry.TrKind = req.TrKind;
            entry.Hectares = req.Hectares;
            entry.HectareRate = req.HectareRate;
            entry.BoxCarryCount = req.BoxCarryCount;
            entry.ExtraNote = req.ExtraNote;

            entry.EntryPay = CalculateEntryPay(entry);

            await db.SaveChangesAsync();
            await RecalculateTimesheetTotals(db, timesheetId);

            return Results.Ok(ToDto(entry));
        })
        .Produces<TimesheetEntryDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .WithOpenApi();

        // DELETE: smazat položku
        grp.MapDelete("/{entryId:guid}", async (Guid timesheetId, Guid entryId, ApplicationDbContext db) =>
        {
            var entry = await db.TimesheetEntries.SingleOrDefaultAsync(e => e.Id == entryId && e.TimesheetId == timesheetId);
            if (entry is null)
                return Results.NotFound(new { error = "Položka nenalezena." });

            // Načíst timesheet pro kontrolu statusu
            var ts = await db.Timesheets.FindAsync(timesheetId);
            if (ts is not null)
            {
                // Kontrola: nelze mazat položky ze schváleného nebo odeslaného výkazu
                if (ts.Status == TimesheetStatus.Approved)
                    return Results.Problem(statusCode: 403, title: "Forbidden", detail: "Schválený výkaz nelze upravovat.");
                
                if (ts.Status == TimesheetStatus.Submitted)
                    return Results.Problem(statusCode: 403, title: "Forbidden", detail: "Odeslaný výkaz nelze upravovat. Nejprve ho vraťte k přepracování.");
            }

            db.TimesheetEntries.Remove(entry);
            await db.SaveChangesAsync();
            await RecalculateTimesheetTotals(db, timesheetId);

            return Results.NoContent();
        })
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound)
        .WithOpenApi();

        return app;
    }

    private static decimal CalculateEntryPay(TimesheetEntry e)
    {
        var basePay = (e.Hours * e.HourRate) + (e.Km * e.KmRate) + (e.Pieces * e.PieceRate);
        var hectarePay = (e.Hectares ?? 0m) * (e.HectareRate ?? 0m);
        e.HectarePay = hectarePay;
        return basePay + hectarePay;
    }

    private static async Task RecalculateTimesheetTotals(ApplicationDbContext db, Guid timesheetId)
    {
        var totals = await db.TimesheetEntries
            .Where(x => x.TimesheetId == timesheetId)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Hours = g.Sum(x => x.Hours),
                Km = g.Sum(x => x.Km),
                Pieces = g.Sum(x => x.Pieces),
                Pay = g.Sum(x => x.EntryPay)
            })
            .SingleOrDefaultAsync();

        var ts = await db.Timesheets.SingleAsync(t => t.Id == timesheetId);
        ts.TotalHours = totals?.Hours ?? 0m;
        ts.TotalKm = totals?.Km ?? 0m;
        ts.TotalPieces = totals?.Pieces ?? 0;
        ts.TotalPay = totals?.Pay ?? 0m;
        await db.SaveChangesAsync();
    }

    private static TimesheetEntryDto ToDto(TimesheetEntry e) =>
        new(
            e.Id,
            e.WorkDate,
            e.Project,
            e.Task,
            e.Hours,
            e.Km,
            e.Pieces,
            e.HourRate,
            e.KmRate,
            e.PieceRate,
            e.EntryPay,
            e.Comment,
            e.FromTime,
            e.ToTime,
            e.PauseMinutes,
            e.TravelMinutes,
            e.AreaCode,
            e.AreaName,
            e.TrKind,
            e.Hectares,
            e.HectareRate,
            e.HectarePay,
            e.BoxCarryCount,
            e.ExtraNote
        );

    // ===== DTOs a requesty (lokálně v souboru, ať nic nechybí) =====

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
        string? Comment,

        // rozšířené sloupce z excelu
        TimeOnly? FromTime,
        TimeOnly? ToTime,
        int? PauseMinutes,
        int? TravelMinutes,
        string? AreaCode,
        string? AreaName,
        string? TrKind,
        decimal? Hectares,
        decimal? HectareRate,
        decimal? HectarePay,
        int? BoxCarryCount,
        string? ExtraNote
    );

    public record TimesheetDetailDto(
        Guid Id,
        string EmployeeId,
        DateOnly PeriodStart,
        DateOnly PeriodEnd,
        string Status,
        string? Notes,
        decimal TotalHours,
        decimal TotalKm,
        int TotalPieces,
        decimal TotalPay,
        DateTime? SubmittedAt,
        DateTime? ApprovedAt,
        List<TimesheetEntryDto> Entries
    );

    public record CreateEntryRequest(
        DateOnly WorkDate,
        string? Project,
        string? Task,
        decimal Hours,
        decimal Km,
        int Pieces,
        decimal HourRate,
        decimal KmRate,
        decimal PieceRate,
        string? Comment,

        // optional/extended
        TimeOnly? FromTime,
        TimeOnly? ToTime,
        int? PauseMinutes,
        int? TravelMinutes,
        string? AreaCode,
        string? AreaName,
        string? TrKind,
        decimal? Hectares,
        decimal? HectareRate,
        int? BoxCarryCount,
        string? ExtraNote
    );

    public record UpdateEntryRequest(
        DateOnly WorkDate,
        string? Project,
        string? Task,
        decimal Hours,
        decimal Km,
        int Pieces,
        decimal HourRate,
        decimal KmRate,
        decimal PieceRate,
        string? Comment,

        // optional/extended
        TimeOnly? FromTime,
        TimeOnly? ToTime,
        int? PauseMinutes,
        int? TravelMinutes,
        string? AreaCode,
        string? AreaName,
        string? TrKind,
        decimal? Hectares,
        decimal? HectareRate,
        int? BoxCarryCount,
        string? ExtraNote
    );
}