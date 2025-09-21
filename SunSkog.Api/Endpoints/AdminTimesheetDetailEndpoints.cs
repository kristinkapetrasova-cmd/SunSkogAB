using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SunSkog.Api.Contracts;
using SunSkog.Api.Data;
using SunSkog.Api.Models.Domain;

namespace SunSkog.Api.Endpoints;

public static class AdminTimesheetDetailEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        // Skupina jen pro nové cesty detailu / entries / export
        var g = app.MapGroup("/api/admin/timesheets").RequireAuthorization();

        // GET /api/admin/timesheets/{id}
        g.MapGet("/{id:guid}", async (Guid id, ApplicationDbContext db) =>
        {
            var q =
                from t in db.Timesheets.AsNoTracking().Include(x => x.Entries)
                where t.Id == id
                join u in db.Users.AsNoTracking() on t.EmployeeId equals u.Id into gj
                from u in gj.DefaultIfEmpty()
                select new AdminTimesheetDetailDto
                {
                    Id            = t.Id,
                    EmployeeEmail = u != null ? u.Email    : null,
                    EmployeeName  = u != null ? u.FullName : null,
                    PeriodStart   = t.PeriodStart,
                    PeriodEnd     = t.PeriodEnd,
                    Status        = (int)t.Status,
                    SubmittedAt   = t.SubmittedAt,
                    ApprovedAt    = t.ApprovedAt,
                    Notes         = t.Notes,
                    TotalHours    = t.TotalHours,
                    TotalKm       = t.TotalKm,
                    TotalPieces   = t.TotalPieces,
                    TotalPay      = t.TotalPay,
                    Entries       = t.Entries
                        .OrderBy(e => e.WorkDate)
                        .Select(e => new AdminTimesheetEntryDto
                        {
                            Id        = e.Id,
                            WorkDate  = e.WorkDate,
                            Project   = e.Project,
                            Task      = e.Task,
                            Hours     = e.Hours,
                            Km        = e.Km,
                            Pieces    = e.Pieces,
                            HourRate  = e.HourRate,
                            KmRate    = e.KmRate,
                            PieceRate = e.PieceRate,
                            EntryPay  = e.EntryPay,
                            Comment   = e.Comment
                        }).ToList()
                };

            var dto = await q.FirstOrDefaultAsync();
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        });

        // GET /api/admin/timesheets/{id}/entries
        g.MapGet("/{id:guid}/entries", async (Guid id, ApplicationDbContext db) =>
        {
            var exists = await db.Timesheets.AsNoTracking().AnyAsync(t => t.Id == id);
            if (!exists) return Results.NotFound();

            var items = await db.Timesheets
                .AsNoTracking()
                .Where(t => t.Id == id)
                .SelectMany(t => t.Entries)
                .OrderBy(e => e.WorkDate)
                .Select(e => new AdminTimesheetEntryDto
                {
                    Id        = e.Id,
                    WorkDate  = e.WorkDate,
                    Project   = e.Project,
                    Task      = e.Task,
                    Hours     = e.Hours,
                    Km        = e.Km,
                    Pieces    = e.Pieces,
                    HourRate  = e.HourRate,
                    KmRate    = e.KmRate,
                    PieceRate = e.PieceRate,
                    EntryPay  = e.EntryPay,
                    Comment   = e.Comment
                })
                .ToListAsync();

            return Results.Ok(items);
        });

        // GET /api/admin/timesheets/{id}/export.csv
        g.MapGet("/{id:guid}/export.csv", async (Guid id, ApplicationDbContext db) =>
        {
            var ts = await db.Timesheets
                .AsNoTracking()
                .Include(t => t.Entries)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (ts is null) return Results.NotFound();

            var user = await db.Users.AsNoTracking()
                         .Where(u => u.Id == ts.EmployeeId)
                         .Select(u => new { u.Email, u.FullName })
                         .FirstOrDefaultAsync();

            // CSV – jednoduchá hlavička + položky
            var sb = new StringBuilder();
            var inv = CultureInfo.InvariantCulture;

            // metadata
            sb.AppendLine("TimesheetId,EmployeeEmail,EmployeeName,PeriodStart,PeriodEnd,Status,SubmittedAt,ApprovedAt,TotalHours,TotalKm,TotalPieces,TotalPay");
            sb.AppendLine(string.Join(",",
                ts.Id,
                Csv(user?.Email),
                Csv(user?.FullName),
                ts.PeriodStart.ToString("yyyy-MM-dd"),
                ts.PeriodEnd.ToString("yyyy-MM-dd"),
                (int)ts.Status,
                ts.SubmittedAt?.ToString("O") ?? "",
                ts.ApprovedAt?.ToString("O") ?? "",
                ts.TotalHours.ToString(inv),
                ts.TotalKm.ToString(inv),
                ts.TotalPieces.ToString(inv),
                ts.TotalPay.ToString(inv)
            ));
            sb.AppendLine();

            // entries
            sb.AppendLine("EntryId,WorkDate,Project,Task,Hours,Km,Pieces,HourRate,KmRate,PieceRate,EntryPay,Comment");
            foreach (var e in ts.Entries.OrderBy(x => x.WorkDate))
            {
                sb.AppendLine(string.Join(",",
                    e.Id,
                    e.WorkDate.ToString("yyyy-MM-dd"),
                    Csv(e.Project),
                    Csv(e.Task),
                    e.Hours.ToString(inv),
                    e.Km.ToString(inv),
                    e.Pieces.ToString(inv),
                    e.HourRate.ToString(inv),
                    e.KmRate.ToString(inv),
                    e.PieceRate.ToString(inv),
                    e.EntryPay.ToString(inv),
                    Csv(e.Comment)
                ));
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            var fileName = $"timesheet_{ts.Id}.csv";
            return Results.File(bytes, "text/csv", fileName);
        });
    }

    private static string Csv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        var v = value.Replace("\"", "\"\"");
        // obalíme uvozovkami kvůli čárkám
        return $"\"{v}\"";
    }
}