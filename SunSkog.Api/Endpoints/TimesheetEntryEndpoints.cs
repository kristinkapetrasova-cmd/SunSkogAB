using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SunSkog.Api.Contracts;
using SunSkog.Api.Data;
using SunSkog.Api.Models.Domain;

namespace SunSkog.Api.Endpoints;

public static class TimesheetEntryEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        // Položky výkazu (chráněno)
        var g = app.MapGroup("/api/timesheets").RequireAuthorization();

        // POST /api/timesheets/{id}/entries  (přidat položku)
        g.MapPost("/{id:guid}/entries", async (Guid id, HttpContext http, ApplicationDbContext db, [FromBody] EntryDto dto) =>
        {
            var userId = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Results.Unauthorized();

            var ts = await db.Timesheets.Include(t => t.Entries).FirstOrDefaultAsync(t => t.Id == id);
            if (ts == null) return Results.NotFound();

            if (ts.EmployeeId != userId && !UserCanApprove(http))
                return Results.Forbid();

            if (ts.Status != TimesheetStatus.Draft)
                return Results.BadRequest(new { message = "Položky lze měnit jen u návrhu (Draft)." });

            var e = new TimesheetEntry
            {
                Id         = Guid.NewGuid(),
                TimesheetId= ts.Id,
                WorkDate   = dto.WorkDate,
                Project    = dto.Project ?? string.Empty,
                Task       = dto.Task ?? string.Empty,
                Hours      = dto.Hours,
                Km         = dto.Km,
                Pieces     = dto.Pieces,
                HourRate   = dto.HourRate,
                KmRate     = dto.KmRate,
                PieceRate  = dto.PieceRate,
                EntryPay   = 0m,
                Comment    = dto.Comment
            };
            e.EntryPay = ComputeEntryPay(e);

            ts.Entries.Add(e);
            RecalcTotals(ts);
            await db.SaveChangesAsync();

            return Results.Created($"/api/timesheets/{ts.Id}", ToDetailDto(ts));
        });

        // PUT /api/timesheets/{id}/entries/{entryId}  (upravit položku)
        g.MapPut("/{id:guid}/entries/{entryId:guid}", async (Guid id, Guid entryId, HttpContext http, ApplicationDbContext db, [FromBody] EntryDto dto) =>
        {
            var userId = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Results.Unauthorized();

            var ts = await db.Timesheets.Include(t => t.Entries).FirstOrDefaultAsync(t => t.Id == id);
            if (ts == null) return Results.NotFound();

            if (ts.EmployeeId != userId && !UserCanApprove(http))
                return Results.Forbid();

            if (ts.Status != TimesheetStatus.Draft)
                return Results.BadRequest(new { message = "Položky lze měnit jen u návrhu (Draft)." });

            var e = ts.Entries.FirstOrDefault(x => x.Id == entryId);
            if (e == null) return Results.NotFound();

            e.WorkDate  = dto.WorkDate;
            e.Project   = dto.Project ?? string.Empty;
            e.Task      = dto.Task ?? string.Empty;
            e.Hours     = dto.Hours;
            e.Km        = dto.Km;
            e.Pieces    = dto.Pieces;
            e.HourRate  = dto.HourRate;
            e.KmRate    = dto.KmRate;
            e.PieceRate = dto.PieceRate;
            e.Comment   = dto.Comment;
            e.EntryPay  = ComputeEntryPay(e);

            RecalcTotals(ts);
            await db.SaveChangesAsync();

            return Results.Ok(ToDetailDto(ts));
        });

        // DELETE /api/timesheets/{id}/entries/{entryId}
        g.MapDelete("/{id:guid}/entries/{entryId:guid}", async (Guid id, Guid entryId, HttpContext http, ApplicationDbContext db) =>
        {
            var userId = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Results.Unauthorized();

            var ts = await db.Timesheets.Include(t => t.Entries).FirstOrDefaultAsync(t => t.Id == id);
            if (ts == null) return Results.NotFound();

            if (ts.EmployeeId != userId && !UserCanApprove(http))
                return Results.Forbid();

            if (ts.Status != TimesheetStatus.Draft)
                return Results.BadRequest(new { message = "Položky lze měnit jen u návrhu (Draft)." });

            var e = ts.Entries.FirstOrDefault(x => x.Id == entryId);
            if (e == null) return Results.NotFound();

            ts.Entries.Remove(e);
            db.TimesheetEntries.Remove(e);

            RecalcTotals(ts);
            await db.SaveChangesAsync();

            return Results.NoContent();
        });
    }

    /* ---------- Helpers (lokální kopie – bez konfliktu s druhou třídou) ---------- */

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
            t.Entries.OrderBy(e => e.WorkDate).Select(e => new TimesheetEntryDetailDto(
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
            )).ToList()
        );
}