using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SunSkog.Api.Data;
using SunSkog.Api.Models.Domain;

namespace SunSkog.Api.Endpoints;

public static class AdminTimesheetExportEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/admin/timesheets").RequireAuthorization();

        // GET /api/admin/timesheets/export.csv
        g.MapGet("/export.csv", async (
            ApplicationDbContext db,
            string? from,
            string? to,
            int? status,
            string? employeeEmail
        ) =>
        {
            // --- Parse filtrů ---
            DateOnly? fromDate = null, toDate = null;
            if (!string.IsNullOrWhiteSpace(from) && DateOnly.TryParse(from, out var f)) fromDate = f;
            if (!string.IsNullOrWhiteSpace(to)   && DateOnly.TryParse(to,   out var tParsed)) toDate = tParsed;

            IQueryable<Models.Domain.Timesheet> q = db.Timesheets
                .AsNoTracking()
                .Include(ts => ts.Entries);

            if (fromDate.HasValue) q = q.Where(ts => ts.PeriodEnd   >= fromDate.Value);
            if (toDate.HasValue)   q = q.Where(ts => ts.PeriodStart <= toDate.Value);

            if (status.HasValue)
            {
                var wanted = (TimesheetStatus)status.Value;
                q = q.Where(ts => ts.Status == wanted);
            }

            if (!string.IsNullOrWhiteSpace(employeeEmail))
            {
                // Přefiltruj dle emailu
                q = q.Join(
                        db.Users.AsNoTracking(),
                        ts => ts.EmployeeId,
                        u  => u.Id,
                        (ts, u) => new { ts, u }
                    )
                    .Where(x => x.u.Email != null && x.u.Email == employeeEmail)
                    .Select(x => x.ts);
            }

            var rows = await q
                .GroupJoin(
                    db.Users.AsNoTracking(),
                    ts => ts.EmployeeId,
                    u  => u.Id,
                    (ts, ug) => new { ts, ug }
                )
                .SelectMany(x => x.ug.DefaultIfEmpty(), (x, u) => new
                {
                    x.ts.Id,
                    EmployeeEmail = u != null ? u.Email    : null,
                    EmployeeName  = u != null ? u.FullName : null,
                    x.ts.PeriodStart,
                    x.ts.PeriodEnd,
                    StatusInt     = (int)x.ts.Status,
                    x.ts.SubmittedAt,
                    x.ts.ApprovedAt,
                    x.ts.Notes,
                    x.ts.TotalHours,
                    x.ts.TotalKm,
                    x.ts.TotalPieces,
                    x.ts.TotalPay
                })
                .OrderByDescending(r => r.PeriodStart)
                .ThenBy(r => r.Id)
                .ToListAsync();

            // --- CSV ---
            var sb  = new StringBuilder();
            var inv = CultureInfo.InvariantCulture;

            // hlavička
            sb.AppendLine("TimesheetId,EmployeeEmail,EmployeeName,PeriodStart,PeriodEnd,Status,SubmittedAt,ApprovedAt,Notes,TotalHours,TotalKm,TotalPieces,TotalPay");

            foreach (var r in rows)
            {
                sb.AppendLine(string.Join(",",
                    r.Id,
                    Csv(r.EmployeeEmail),
                    Csv(r.EmployeeName),
                    r.PeriodStart.ToString("yyyy-MM-dd"),
                    r.PeriodEnd.ToString("yyyy-MM-dd"),
                    r.StatusInt.ToString(inv),
                    r.SubmittedAt?.ToString("O") ?? "",
                    r.ApprovedAt?.ToString("O") ?? "",
                    Csv(r.Notes),
                    r.TotalHours.ToString(inv),
                    r.TotalKm.ToString(inv),
                    r.TotalPieces.ToString(inv),
                    r.TotalPay.ToString(inv)
                ));
            }

            // UTF-8 BOM kvůli Excelu
            var bom   = Encoding.UTF8.GetPreamble();
            var csv   = Encoding.UTF8.GetBytes(sb.ToString());
            var bytes = new byte[bom.Length + csv.Length];
            Buffer.BlockCopy(bom, 0, bytes, 0, bom.Length);
            Buffer.BlockCopy(csv, 0, bytes, bom.Length, csv.Length);

            var fileName = BuildName(fromDate, toDate, status, employeeEmail);
            return Results.File(bytes, "text/csv", fileName);
        });
    }

    private static string Csv(string? v)
    {
        if (string.IsNullOrEmpty(v)) return "";
        return $"\"{v.Replace("\"", "\"\"")}\"";
    }

    private static string BuildName(DateOnly? from, DateOnly? to, int? status, string? email)
    {
        var parts = new List<string> { "timesheets" };
        if (from.HasValue)            parts.Add($"from_{from.Value:yyyyMMdd}");
        if (to.HasValue)              parts.Add($"to_{to.Value:yyyyMMdd}");
        if (status.HasValue)          parts.Add($"status_{status.Value}");
        if (!string.IsNullOrWhiteSpace(email)) parts.Add($"emp_{San(email)}");
        parts.Add(DateTime.UtcNow.ToString("yyyyMMddHHmmss"));
        return string.Join("_", parts) + ".csv";
    }

    private static string San(string s)
    {
        var bad = Path.GetInvalidFileNameChars();
        return new string(s.Select(ch => bad.Contains(ch) ? '_' : ch).ToArray());
    }
}