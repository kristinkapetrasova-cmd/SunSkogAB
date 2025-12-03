using System.Globalization;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SunSkog.Api.Data;

namespace SunSkog.Api.Endpoints;

public static class AdminTimesheetCsvExportEndpoints
{
    // !!! PřEJMENOVANÁ extension metoda (aby se nekřížila s AdminExportEndpoints) !!!
    public static IEndpointRouteBuilder MapAdminTimesheetCsvExportEndpoints(this IEndpointRouteBuilder app)
    {
        var grp = app.MapGroup("/api/admin/export")
            .WithTags("Admin - Export");

        // Diagnostika přihlášeného uživatele (pro kontrolu rolí/claimů)
        grp.MapGet("/whoami", [Authorize] (ClaimsPrincipal user) =>
        {
            var name  = user.Identity?.Name ?? "";
            var roles = user.Claims
                .Where(c => c.Type == ClaimTypes.Role || c.Type.EndsWith("/claims/role"))
                .Select(c => c.Value)
                .Distinct()
                .ToArray();
            return Results.Ok(new { name, roles });
        })
        .WithOpenApi();

        // 1) Přehled timesheetů (jeden řádek = jeden timesheet)
        grp.MapGet("/timesheets.csv", ExportTimesheetsCsv)
           .RequireAuthorization(new AuthorizeAttribute { Roles = "Admin,Accountant,Management" })
           .Produces<string>(contentType: "text/csv")
           .WithOpenApi();

        // 2) Detaily položek (jeden řádek = jedna položka výkazu)
        grp.MapGet("/timesheets-details.csv", ExportTimesheetsDetailsCsv)
           .RequireAuthorization(new AuthorizeAttribute { Roles = "Admin,Accountant,Management" })
           .Produces<string>(contentType: "text/csv")
           .WithOpenApi();

        return app;
    }

    // -------- Přehled (1 řádek = 1 timesheet) --------
    private static async Task<IResult> ExportTimesheetsCsv(
        ApplicationDbContext db,
        string? from,
        string? to,
        CancellationToken ct)
    {
        var (dFrom, dTo) = GetRangeOrDefault(from, to);

        // bereme timesheety, které mají v období alespoň jednu položku
        var tsIdsInRange = await db.TimesheetEntries
            .AsNoTracking()
            .Where(e => e.WorkDate >= dFrom && e.WorkDate <= dTo)
            .Select(e => e.TimesheetId)
            .Distinct()
            .ToListAsync(ct);

        var timesheets = await db.Timesheets
            .AsNoTracking()
            .Where(t => tsIdsInRange.Contains(t.Id))
            .OrderByDescending(t => t.PeriodStart)
            .ToListAsync(ct);

        var employeeIds = timesheets.Select(t => t.EmployeeId).Distinct().ToList();
        var userLookup = await db.Users
            .Where(u => employeeIds.Contains(u.Id))
            .Select(u => new { u.Id, u.Email, u.FullName })
            .ToDictionaryAsync(x => x.Id, x => (x.Email ?? "", x.FullName ?? ""), ct);

        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",",
            "TimesheetId","EmployeeId","EmployeeEmail","EmployeeName",
            "PeriodStart","PeriodEnd","Status","SubmittedAt","ApprovedAt",
            "TotalHours","TotalKm","TotalPieces","TotalPay"
        ));

        foreach (var t in timesheets)
        {
            var (mail, name) = userLookup.TryGetValue(t.EmployeeId, out var v) ? v : ("", "");
            sb.AppendLine(string.Join(",",
                Csv(t.Id),
                Csv(t.EmployeeId),
                Csv(mail),
                Csv(name),
                Csv(t.PeriodStart),
                Csv(t.PeriodEnd),
                Csv(t.Status.ToString()),
                Csv(t.SubmittedAt),
                Csv(t.ApprovedAt),
                Csv(t.TotalHours),
                Csv(t.TotalKm),
                Csv(t.TotalPieces),
                Csv(t.TotalPay)
            ));
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        var fileName = $"timesheets_{dFrom:yyyy-MM-dd}_{dTo:yyyy-MM-dd}.csv";
        return Results.File(bytes, "text/csv; charset=utf-8", fileName);
    }

    // -------- Detaily (1 řádek = 1 entry) --------
    private static async Task<IResult> ExportTimesheetsDetailsCsv(
        ApplicationDbContext db,
        string? from,
        string? to,
        CancellationToken ct)
    {
        var (dFrom, dTo) = GetRangeOrDefault(from, to);

        var q = from e in db.TimesheetEntries.AsNoTracking()
                join t in db.Timesheets.AsNoTracking() on e.TimesheetId equals t.Id
                where e.WorkDate >= dFrom && e.WorkDate <= dTo
                orderby e.WorkDate, t.Id
                select new
                {
                    t.Id,
                    t.EmployeeId,
                    t.PeriodStart,
                    t.PeriodEnd,
                    t.Status,
                    t.SubmittedAt,
                    t.ApprovedAt,
                    t.TotalHours,
                    t.TotalKm,
                    t.TotalPieces,
                    t.TotalPay,
                    EntryId = e.Id,
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
                };

        var rows = await q.ToListAsync(ct);

        var employeeIds = rows.Select(r => r.EmployeeId).Distinct().ToList();
        var userLookup = await db.Users
            .Where(u => employeeIds.Contains(u.Id))
            .Select(u => new { u.Id, u.Email, u.FullName })
            .ToDictionaryAsync(x => x.Id, x => (x.Email ?? "", x.FullName ?? ""), ct);

        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",",
            "TimesheetId","EmployeeId","EmployeeEmail","EmployeeName",
            "PeriodStart","PeriodEnd","Status","SubmittedAt","ApprovedAt",
            "TotalHours","TotalKm","TotalPieces","TotalPay",
            "EntryId","WorkDate","Project","Task","Hours","Km","Pieces",
            "HourRate","KmRate","PieceRate","EntryPay","Comment"
        ));

        foreach (var r in rows)
        {
            var (mail, name) = userLookup.TryGetValue(r.EmployeeId, out var v) ? v : ("", "");
            sb.AppendLine(string.Join(",",
                Csv(r.Id),
                Csv(r.EmployeeId),
                Csv(mail),
                Csv(name),
                Csv(r.PeriodStart),
                Csv(r.PeriodEnd),
                Csv(r.Status.ToString()),
                Csv(r.SubmittedAt),
                Csv(r.ApprovedAt),
                Csv(r.TotalHours),
                Csv(r.TotalKm),
                Csv(r.TotalPieces),
                Csv(r.TotalPay),
                Csv(r.EntryId),
                Csv(r.WorkDate),
                Csv(r.Project),
                Csv(r.Task),
                Csv(r.Hours),
                Csv(r.Km),
                Csv(r.Pieces),
                Csv(r.HourRate),
                Csv(r.KmRate),
                Csv(r.PieceRate),
                Csv(r.EntryPay),
                Csv(r.Comment)
            ));
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        var fileName = $"timesheets_details_{dFrom:yyyy-MM-dd}_{dTo:yyyy-MM-dd}.csv";
        return Results.File(bytes, "text/csv; charset=utf-8", fileName);
    }

    // ---------- Helpers ----------
    private static (DateOnly From, DateOnly To) GetRangeOrDefault(string? from, string? to)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var firstOfMonth = new DateOnly(today.Year, today.Month, 1);

        var fmt = CultureInfo.InvariantCulture;
        var okFrom = DateOnly.TryParse(from, fmt, DateTimeStyles.None, out var dFrom);
        var okTo   = DateOnly.TryParse(to,   fmt, DateTimeStyles.None, out var dTo);

        if (!okFrom && !okTo) return (firstOfMonth, today);
        if (!okFrom && okTo)  return (new DateOnly(dTo.Year, dTo.Month, 1), dTo);
        if (okFrom && !okTo)  return (dFrom, today);
        if (dFrom > dTo)      return (dTo, dFrom);
        return (dFrom, dTo);
    }

    private static string Csv(object? v)
    {
        if (v is null) return "";
        return v switch
        {
            DateTime dt => Quote(dt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)),
            DateOnly d  => Quote(d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
            decimal m   => Quote(m.ToString(CultureInfo.InvariantCulture)),
            double d2   => Quote(d2.ToString(CultureInfo.InvariantCulture)),
            float f     => Quote(f.ToString(CultureInfo.InvariantCulture)),
            int i       => Quote(i.ToString(CultureInfo.InvariantCulture)),
            long l      => Quote(l.ToString(CultureInfo.InvariantCulture)),
            _           => Quote(v.ToString() ?? "")
        };

        static string Quote(string s)
        {
            var escaped = s.Replace("\"", "\"\"");
            return $"\"{escaped}\"";
        }
    }
}