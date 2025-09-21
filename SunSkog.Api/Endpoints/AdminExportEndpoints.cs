using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SunSkog.Api.Data;
using SunSkog.Api.Models.Domain;

namespace SunSkog.Api.Endpoints;

public static class AdminExportEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        // Skupina pro admin routy (naváže se na /api/admin/timesheets)
        var g = app.MapGroup("/api/admin/timesheets").RequireAuthorization();

        // GET /api/admin/timesheets/export?from=YYYY-MM-DD&to=YYYY-MM-DD&status=2&employeeEmail=...
        g.MapGet("/export", async (
            [FromServices] ApplicationDbContext db,
            string? from,
            string? to,
            int? status,
            string? employeeEmail
        ) =>
        {
            // 1) Parsování dat (volitelná)
            DateOnly? dFrom = TryParseDateOnly(from);
            DateOnly? dTo   = TryParseDateOnly(to);

            // 2) Join na uživatele, abychom měli jméno + email a mohli filtrovat
            var query = from t in db.Timesheets.AsNoTracking()
                        join u in db.Users.AsNoTracking() on t.EmployeeId equals u.Id into gj
                        from u in gj.DefaultIfEmpty()
                        select new
                        {
                            T = t,
                            U = u
                        };

            // 3) Filtry
            if (dFrom.HasValue)   query = query.Where(x => x.T.PeriodStart >= dFrom.Value);
            if (dTo.HasValue)     query = query.Where(x => x.T.PeriodEnd   <= dTo.Value);
            if (status.HasValue)  query = query.Where(x => x.T.Status == (TimesheetStatus)status.Value);
            if (!string.IsNullOrWhiteSpace(employeeEmail))
                query = query.Where(x => x.U != null && x.U.Email == employeeEmail);

            // 4) Výběr sloupců pro CSV
            var rows = await query
                .OrderBy(x => x.T.PeriodStart)
                .Select(x => new
                {
                    x.T.Id,
                    EmployeeEmail = x.U != null ? x.U.Email : "",
                    EmployeeName  = x.U != null ? x.U.FullName : "",
                    x.T.PeriodStart,
                    x.T.PeriodEnd,
                    StatusText    = x.T.Status.ToString(),
                    x.T.SubmittedAt,
                    x.T.ApprovedAt,
                    x.T.TotalHours,
                    x.T.TotalKm,
                    x.T.TotalPieces,
                    x.T.TotalPay
                })
                .ToListAsync();

            // 5) Sestavení CSV (použijeme středník, aby to šlo pěkně otevřít v českém Excelu)
            var sb = new StringBuilder();

            // Hlavička
            sb.AppendLine(string.Join(';', new[]
            {
                "Id","EmployeeEmail","EmployeeName","PeriodStart","PeriodEnd","Status",
                "SubmittedAt","ApprovedAt","TotalHours","TotalKm","TotalPieces","TotalPay"
            }));

            // Řádky
            foreach (var r in rows)
            {
                var line = string.Join(';', new[]
                {
                    Csv(r.Id.ToString()),
                    Csv(r.EmployeeEmail),
                    Csv(r.EmployeeName),
                    Csv(r.PeriodStart.ToString("yyyy-MM-dd")),
                    Csv(r.PeriodEnd.ToString("yyyy-MM-dd")),
                    Csv(r.StatusText),
                    Csv(r.SubmittedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? ""),
                    Csv(r.ApprovedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? ""),
                    Csv(r.TotalHours.ToString(CultureInfo.InvariantCulture)),
                    Csv(r.TotalKm.ToString(CultureInfo.InvariantCulture)),
                    Csv(r.TotalPieces.ToString(CultureInfo.InvariantCulture)),
                    Csv(r.TotalPay.ToString(CultureInfo.InvariantCulture))
                });
                sb.AppendLine(line);
            }

            // 6) Vrátíme jako soubor (s UTF-8 BOM kvůli Excelu na Windows)
            var csvText = sb.ToString();
            var utf8Bom = Encoding.UTF8.GetPreamble();
            var csvBytes = Encoding.UTF8.GetBytes(csvText);
            var withBom = new byte[utf8Bom.Length + csvBytes.Length];
            Buffer.BlockCopy(utf8Bom, 0, withBom, 0, utf8Bom.Length);
            Buffer.BlockCopy(csvBytes, 0, withBom, utf8Bom.Length, csvBytes.Length);

            var fileName = $"timesheets_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            return Results.File(withBom, "text/csv; charset=utf-8", fileName);
        });
    }

    private static DateOnly? TryParseDateOnly(string? value)
        => DateOnly.TryParse(value, out var d) ? d : (DateOnly?)null;

    private static string Csv(string? s)
    {
        s ??= string.Empty;
        // Ořežeme CR/LF a escapujeme uvozovky
        s = s.Replace("\r", " ").Replace("\n", " ");
        s = s.Replace("\"", "\"\"");
        // Vždy do uvozovek kvůli středníku a případným mezerám
        return $"\"{s}\"";
    }
}