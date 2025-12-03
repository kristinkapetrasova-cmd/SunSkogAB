using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SunSkog.Api.Data;
using SunSkog.Api.Storage.Entities;
using StorageStatus = SunSkog.Api.Storage.Entities.TimesheetStatus;

namespace SunSkog.Api.Endpoints;

public static class AdminTimesheetDetailEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/admin/timesheet-detail")
                   .RequireAuthorization(policy => policy.RequireRole("Manager", "SuperAdmin"))
                   .WithTags("Admin: Timesheet Detail");

        // GET /api/admin/timesheet-detail/{id}
        g.MapGet("{id:guid}", async (Guid id, ApplicationDbContext db) =>
        {
            var t = await db.Timesheets
                .Include(x => x.Entries)
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == id);

            if (t is null) return Results.NotFound();

            var dto = new AdminTimesheetDetailDto(
                t.Id,
                t.EmployeeId,
                t.PeriodStart,
                t.PeriodEnd,
                (int)t.Status,
                t.Notes,
                t.TotalHours,
                t.TotalKm,
                t.TotalPieces,
                t.TotalPay,
                t.SubmittedAt,
                t.ApprovedAt,
                t.Entries
                    .OrderBy(e => e.WorkDate)
                    .Select(e => new AdminTimesheetEntryDto(
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
                        e.Comment
                    ))
                    .ToList()
            );

            return Results.Ok(dto);
        })
        .Produces<AdminTimesheetDetailDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        // POST /api/admin/timesheet-detail/{id}/approve
        g.MapPost("{id:guid}/approve", async (Guid id, ApplicationDbContext db) =>
        {
            var t = await db.Timesheets.SingleOrDefaultAsync(x => x.Id == id);
            if (t is null) return Results.NotFound();

            t.Status = StorageStatus.Approved;
            t.ApprovedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound);

        // POST /api/admin/timesheet-detail/{id}/return
        g.MapPost("{id:guid}/return", async (Guid id, ApplicationDbContext db) =>
        {
            var t = await db.Timesheets.SingleOrDefaultAsync(x => x.Id == id);
            if (t is null) return Results.NotFound();

            t.Status = StorageStatus.Returned;
            t.ApprovedAt = null;

            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound);
    }

    // === Admin DTOs (jiné názvy než veřejné DTOs, ať se to nebije) ===
    public sealed record AdminTimesheetDetailDto(
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
        List<AdminTimesheetEntryDto> Entries
    );

    public sealed record AdminTimesheetEntryDto(
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
}