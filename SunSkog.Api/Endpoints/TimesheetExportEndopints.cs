using System.Globalization;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SunSkog.Api.Data;
using SunSkog.Api.Models.Domain;

namespace SunSkog.Api.Endpoints;

public static class TimesheetExportEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        // stejný prefix jako u TimesheetEndpoints – přidáváme jen nové routy
        var g = app.MapGroup("/api/timesheets").RequireAuthorization();

        // GET /api/timesheets/{id}/csv  – CSV pro jeden výkaz
        g.MapGet("/{id:guid}/csv", async (Guid id, HttpContext http, ApplicationDbContext db) =>
        {
            var userId = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Results.Unauthorized();

            var ts = await db.Timesheets
                .Include(t => t.Entries)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (ts == null) return Results.NotFound();

            // Přístup: vlastník nebo Approver (stejně jako u detailu)
            if (ts.EmployeeId != userId && !UserCanApprove(http))
                return Results.Forbid();

            var csv = BuildSingleTimesheetCsv(ts);
            var fileName = $"timesheet_{ts.PeriodStart:yyyyMMdd}_{ts.PeriodEnd:yyyyMMdd}_{ts.Id}.csv";

            // UTF-8 BOM kvůli Excelu
            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray();
            return Results.File(bytes, "text/csv; charset=utf-8", fileName);
        });

        // GET /api/timesheets/my/export?from=YYYY-MM-DD&to=YYYY-MM-DD&status=0..3
        g.MapGet("/my/export", async (HttpContext http, ApplicationDbContext db, string? from, string? to, int? status) =>
        {
            var userId = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Results.Unauthorized();

            DateOnly? fromDate = ParseDateOnly(from);
            DateOnly? toDate   = ParseDateOnly(to);

            var q = db.Timesheets
                .AsNoTracking()
                .Include(t => t.Entries)
                .Where(t => t.EmployeeId == userId);

            if (fromDate.HasValue) q = q.Where(t => t.PeriodStart >= fromDate.Value);
            if (toDate.HasValue)   q = q.Where(t => t.PeriodEnd   <= toDate.Value);
            if (status.HasValue)   q = q.Where(t => (int)t.Status == status.Value);

            var list = await q.OrderByDescending(t => t.PeriodStart).ToListAsync();

            var csv = BuildMyExportCsv(list);
            var suffix =
                $"{(fromDate.HasValue ? fromDate.Value.ToString("yyyyMMdd") : "all")}_{(toDate.HasValue ? toDate.Value.ToString("yyyyMMdd") : "all")}" +
                (status.HasValue ? $"_status{status.Value}" : "");
            var fileName = $"timesheets_my_{suffix}.csv";

            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray();
            return Results.File(bytes, "text/csv; charset=utf-8", fileName);
        });
    }

    private static bool UserCanApprove(HttpContext http)
        => http.User.IsInRole("Manager") || http.User.IsInRole("SuperAdmin");

    private static DateOnly? ParseDateOnly(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        // očekáváno "yyyy-MM-dd"
        if (DateOnly.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            return d;
        return null;
    }

    private static string Csv(string? s)
    {
        if (string.IsNullOrEmpty(s)) return "\"\"";
        // escapování uvozovek
        var esc = s.Replace("\"", "\"\"");
        return $"\"{esc}\"";
    }

    private static string Csv(decimal v) => v.ToString(CultureInfo.InvariantCulture);
    private static string Csv(int v)     => v.ToString(CultureInfo.InvariantCulture);
    private static string Csv(DateOnly d)=> d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string BuildSingleTimesheetCsv(Timesheet ts)
    {
        var sb = new StringBuilder();

        // hlavička
        sb.AppendLine("Id;EmployeeId;PeriodStart;PeriodEnd;Status;SubmittedAt;ApprovedAt;Notes;TotalHours;TotalKm;TotalPieces;TotalPay");
        sb.AppendLine(string.Join(";", new[]
        {
            ts.Id.ToString(),
            Csv(ts.EmployeeId ?? ""),
            Csv(ts.PeriodStart),
            Csv(ts.PeriodEnd),
            Csv(ts.Status.ToString()),
            Csv(ts.SubmittedAt?.ToString("u") ?? ""),
            Csv(ts.ApprovedAt?.ToString("u") ?? ""),
            Csv(ts.Notes ?? ""),
            Csv(ts.TotalHours),
            Csv(ts.TotalKm),
            Csv(ts.TotalPieces),
            Csv(ts.TotalPay)
        }));

        sb.AppendLine(); // prázdný řádek
        sb.AppendLine("Entries:");
        sb.AppendLine("WorkDate;Project;Task;Hours;Km;Pieces;HourRate;KmRate;PieceRate;EntryPay;Comment");

        foreach (var e in ts.Entries.OrderBy(e => e.WorkDate))
        {
            sb.AppendLine(string.Join(";", new[]
            {
                Csv(e.WorkDate),
                Csv(e.Project ?? ""),
                Csv(e.Task ?? ""),
                Csv(e.Hours),
                Csv(e.Km),
                Csv(e.Pieces),
                Csv(e.HourRate),
                Csv(e.KmRate),
                Csv(e.PieceRate),
                Csv(e.EntryPay),
                Csv(e.Comment ?? "")
            }));
        }

        return sb.ToString();
    }

    private static string BuildMyExportCsv(List<Timesheet> list)
    {
        var sb = new StringBuilder();
        sb.AppendLine("TimesheetId;PeriodStart;PeriodEnd;Status;WorkDate;Project;Task;Hours;Km;Pieces;HourRate;KmRate;PieceRate;EntryPay;Comment");

        foreach (var ts in list.OrderBy(t => t.PeriodStart))
        {
            if (ts.Entries.Count == 0)
            {
                // i prázdné výkazy chceme vidět – aspoň jeden řádek bez entry
                sb.AppendLine(string.Join(";", new[]
                {
                    ts.Id.ToString(),
                    Csv(ts.PeriodStart),
                    Csv(ts.PeriodEnd),
                    Csv(ts.Status.ToString()),
                    "", "", "", "0", "0", "0", "0", "0", "0", "0", ""
                }));
                continue;
            }

            foreach (var e in ts.Entries.OrderBy(e => e.WorkDate))
            {
                sb.AppendLine(string.Join(";", new[]
                {
                    ts.Id.ToString(),
                    Csv(ts.PeriodStart),
                    Csv(ts.PeriodEnd),
                    Csv(ts.Status.ToString()),
                    Csv(e.WorkDate),
                    Csv(e.Project ?? ""),
                    Csv(e.Task ?? ""),
                    Csv(e.Hours),
                    Csv(e.Km),
                    Csv(e.Pieces),
                    Csv(e.HourRate),
                    Csv(e.KmRate),
                    Csv(e.PieceRate),
                    Csv(e.EntryPay),
                    Csv(e.Comment ?? "")
                }));
            }

            // souhrnný řádek za výkaz
            sb.AppendLine(string.Join(";", new[]
            {
                ts.Id.ToString(),
                Csv(ts.PeriodStart),
                Csv(ts.PeriodEnd),
                Csv($"TOTAL_{ts.Status}"),
                "", "", "",
                Csv(ts.TotalHours),
                Csv(ts.TotalKm),
                Csv(ts.TotalPieces),
                "","","",
                Csv(ts.TotalPay),
                ""
            }));
        }

        return sb.ToString();
    }
}