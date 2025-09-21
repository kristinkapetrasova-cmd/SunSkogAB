using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SunSkog.Api.Contracts;
using SunSkog.Api.Data;
using SunSkog.Api.Models.Domain;

namespace SunSkog.Api.Endpoints;

public static class TimesheetEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        // /api/timesheets (chráněno)
        var g = app.MapGroup("/api/timesheets").RequireAuthorization();

        // GET /api/timesheets/my
        g.MapGet("/my", async (HttpContext http, ApplicationDbContext db) =>
        {
            var userId = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Results.Unauthorized();

            var items = await db.Timesheets
                .AsNoTracking()
                .Include(t => t.Entries)
                .Where(t => t.EmployeeId == userId)
                .OrderByDescending(t => t.PeriodStart)
                .Select(t => ToListItemDto(t))
                .ToListAsync();

            return Results.Ok(items);
        });

        // GET /api/timesheets/{id}
        g.MapGet("/{id:guid}", async (Guid id, HttpContext http, ApplicationDbContext db) =>
        {
            var userId = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Results.Unauthorized();

            var ts = await db.Timesheets
                .Include(t => t.Entries)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (ts == null) return Results.NotFound();

            if (ts.EmployeeId != userId && !UserCanApprove(http))
                return Results.Forbid();

            return Results.Ok(ToDetailDto(ts));
        });

        // POST /api/timesheets   (BEZ trailing slash)
        g.MapPost("", async (HttpContext http, ApplicationDbContext db, [FromBody] CreateDto dto) =>
        {
            var userId = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Results.Unauthorized();

            // vytvoření výkazu
            var ts = new Timesheet
            {
                Id          = Guid.NewGuid(),
                EmployeeId  = userId,
                PeriodStart = dto.PeriodStart,
                PeriodEnd   = dto.PeriodEnd,
                Status      = TimesheetStatus.Draft,
                Notes       = null,
                SubmittedAt = null,
                ApprovedAt  = null,
                Entries     = new List<TimesheetEntry>()
            };

            if (dto.Entries != null)
            {
                foreach (var e in dto.Entries)
                {
                    var entry = new TimesheetEntry
                    {
                        Id         = Guid.NewGuid(),
                        TimesheetId= ts.Id,
                        WorkDate   = e.WorkDate,
                        Project    = e.Project ?? string.Empty,
                        Task       = e.Task ?? string.Empty,
                        Hours      = e.Hours,
                        Km         = e.Km,
                        Pieces     = e.Pieces,
                        HourRate   = e.HourRate,
                        KmRate     = e.KmRate,
                        PieceRate  = e.PieceRate,
                        EntryPay   = 0m,
                        Comment    = e.Comment
                    };
                    entry.EntryPay = ComputeEntryPay(entry);
                    ts.Entries.Add(entry);
                }
            }

            RecalcTotals(ts);
            db.Timesheets.Add(ts);
            await db.SaveChangesAsync();

            var dtoOut = ToDetailDto(ts);
            return Results.Created($"/api/timesheets/{ts.Id}", dtoOut);
        });

        // POST /api/timesheets/{id}/submit
        g.MapPost("/{id:guid}/submit", async (Guid id, HttpContext http, ApplicationDbContext db) =>
        {
            var userId = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Results.Unauthorized();

            var ts = await db.Timesheets.Include(t => t.Entries).FirstOrDefaultAsync(t => t.Id == id);
            if (ts == null) return Results.NotFound();

            if (ts.EmployeeId != userId && !UserCanApprove(http))
                return Results.Forbid();

            ts.Status      = TimesheetStatus.Submitted;
            ts.SubmittedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return Results.Ok(ToDetailDto(ts));
        });

        // POST /api/timesheets/{id}/approve
        g.MapPost("/{id:guid}/approve", async (Guid id, HttpContext http, ApplicationDbContext db, [FromBody] NoteDto dto) =>
        {
            if (!UserCanApprove(http))
                return Results.Forbid();

            var ts = await db.Timesheets.Include(t => t.Entries).FirstOrDefaultAsync(t => t.Id == id);
            if (ts == null) return Results.NotFound();

            ts.Status     = TimesheetStatus.Approved;
            ts.ApprovedAt = DateTime.UtcNow;
            if (!string.IsNullOrWhiteSpace(dto?.Notes)) ts.Notes = dto.Notes;

            await db.SaveChangesAsync();
            return Results.Ok(ToDetailDto(ts));
        });

        // POST /api/timesheets/{id}/return
        g.MapPost("/{id:guid}/return", async (Guid id, HttpContext http, ApplicationDbContext db, [FromBody] NoteDto dto) =>
        {
            if (!UserCanApprove(http))
                return Results.Forbid();

            var ts = await db.Timesheets.Include(t => t.Entries).FirstOrDefaultAsync(t => t.Id == id);
            if (ts == null) return Results.NotFound();

            ts.Status = TimesheetStatus.Returned;
            if (!string.IsNullOrWhiteSpace(dto?.Notes)) ts.Notes = dto.Notes;

            await db.SaveChangesAsync();
            return Results.Ok(ToDetailDto(ts));
        });
    }

    /* ---------- Helpers ---------- */

    private static bool UserCanApprove(HttpContext http) =>
        http.User.IsInRole("SuperAdmin") || http.User.IsInRole("Manager") || http.User.IsInRole("Admin");

    private static decimal ComputeEntryPay(TimesheetEntry e) =>
        (e.Hours   * e.HourRate)
      + (e.Km      * e.KmRate)
      + (e.Pieces  * e.PieceRate);

    private static void RecalcTotals(Timesheet ts)
    {
        ts.TotalHours  = ts.Entries.Sum(x => x.Hours);
        ts.TotalKm     = ts.Entries.Sum(x => x.Km);
        ts.TotalPieces = ts.Entries.Sum(x => x.Pieces);
        ts.TotalPay    = ts.Entries.Sum(x => x.EntryPay);
    }

    private static TimesheetListItemDto ToListItemDto(Timesheet t) =>
        new TimesheetListItemDto(
            t.Id,
            t.EmployeeId ?? string.Empty,
            t.PeriodStart,
            t.PeriodEnd,
            (int)t.Status,
            t.SubmittedAt,
            t.ApprovedAt,
            t.Notes,
            t.TotalHours,
            t.TotalKm,
            t.TotalPieces,
            t.TotalPay
        );

    private static TimesheetDetailDto ToDetailDto(Timesheet t) =>
        new TimesheetDetailDto(
            t.Id,
            t.EmployeeId ?? string.Empty,
            t.PeriodStart,
            t.PeriodEnd,
            (int)t.Status,
            t.SubmittedAt,
            t.ApprovedAt,
            t.Notes,
            t.TotalHours,
            t.TotalKm,
            t.TotalPieces,
            t.TotalPay,
            t.Entries.OrderBy(e => e.WorkDate).Select(ToDetailDto).ToList()
        );

    private static TimesheetEntryDetailDto ToDetailDto(TimesheetEntry e) =>
        new TimesheetEntryDetailDto(
            e.Id,
            e.WorkDate,
            e.Project ?? string.Empty,
            e.Task ?? string.Empty,
            e.Hours,
            e.Km,
            e.Pieces,
            e.HourRate,
            e.KmRate,
            e.PieceRate,
            e.EntryPay,
            e.Comment
        );
}