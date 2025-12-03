using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SunSkog.Api.Data;

namespace SunSkog.Api.Endpoints;

public static class AdminExportEndpoints
{
    public static IEndpointRouteBuilder MapAdminExportEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/admin/export")
                   .WithTags("AdminExport")
                   .RequireAuthorization();

        // CSV – souhrnné řádky (timesheety)
        g.MapGet("/timesheets.csv", ExportTimesheetsCsv)
         .Produces(StatusCodes.Status200OK, contentType: "text/csv")
         .WithName("AdminExportTimesheetsCsv")
         .WithOpenApi();

        // CSV – detailní řádky (entries)
        g.MapGet("/timesheets-details.csv", ExportTimesheetsDetailsCsv)
         .Produces(StatusCodes.Status200OK, contentType: "text/csv")
         .WithName("AdminExportTimesheetsDetailsCsv")
         .WithOpenApi();

        return app;
    }

    private static bool UserCanExport(HttpContext http)
        => http.User.IsInRole("Manager") || http.User.IsInRole("SuperAdmin") || http.User.IsInRole("Accountant");

    private static async Task<IResult> ExportTimesheetsCsv(
        HttpContext http,
        ApplicationDbContext db,
        string? from,
        string? to,
        int? status,
        string? employeeEmail)
    {
        if (!UserCanExport(http)) return Results.Forbid();

        var fromDate = ParseDateOnly(from);
        var toDate   = ParseDateOnly(to);

        var q = db.Timesheets.AsNoTracking();

        if (fromDate.HasValue) q = q.Where(t => t.PeriodStart >= fromDate.Value);
        if (toDate.HasValue)   q = q.Where(t => t.PeriodEnd   <= toDate.Value);
        if (status.HasValue)   q = q.Where(t => (int)t.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(employeeEmail))
        {
            var uid = await db.Users
                .Where(u => u.Email == employeeEmail)
                .Select(u => u.Id)
                .FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(uid))
            {
                // prázdný CSV s hlavičkou
                var empty = "TimesheetId,EmployeeEmail,EmployeeName,PeriodStart,PeriodEnd,Status,StatusName,SubmittedAt,ApprovedAt,TotalHours,TotalKm,TotalPieces,TotalPay\r\n";
                return Results.File(Encoding.UTF8.GetBytes(empty), "text/csv", "timesheets.csv");
            }

            q = q.Where(t => t.EmployeeId == uid);
        }

        // join na Users pro email/jméno
        var data = await (
            from t in q
            join u in db.Users on t.EmployeeId equals u.Id into gj
            from u in gj.DefaultIfEmpty()
            orderby t.PeriodStart descending
            select new
            {
                t.Id,
                EmployeeEmail = u != null ? (u.Email ?? "") : "",
                EmployeeName  = u != null ? (u.FullName ?? "") : "",
                t.PeriodStart,
                t.PeriodEnd,
                Status      = (int)t.Status,
                StatusName  = t.Status.ToString(),
                t.SubmittedAt,
                t.ApprovedAt,
                t.TotalHours,
                t.TotalKm,
                t.TotalPieces,
                t.TotalPay
            }
        ).ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("TimesheetId,EmployeeEmail,EmployeeName,PeriodStart,PeriodEnd,Status,StatusName,SubmittedAt,ApprovedAt,TotalHours,TotalKm,TotalPieces,TotalPay");

        foreach (var r in data)
        {
            sb.Append(r.Id).Append(',');
            sb.Append(Escape(r.EmployeeEmail)).Append(',');
            sb.Append(Escape(r.EmployeeName)).Append(',');
            sb.Append(FormatDateOnly(r.PeriodStart)).Append(',');
            sb.Append(FormatDateOnly(r.PeriodEnd)).Append(',');
            sb.Append(r.Status).Append(',');
            sb.Append(Escape(r.StatusName)).Append(',');
            sb.Append(FormatDateTime(r.SubmittedAt)).Append(',');
            sb.Append(FormatDateTime(r.ApprovedAt)).Append(',');
            sb.Append(FormatDecimal(r.TotalHours)).Append(',');
            sb.Append(FormatDecimal(r.TotalKm)).Append(',');
            sb.Append(r.TotalPieces).Append(',');
            sb.AppendLine(FormatDecimal(r.TotalPay));
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return Results.File(bytes, "text/csv", "timesheets.csv");
    }

    private static async Task<IResult> ExportTimesheetsDetailsCsv(
        HttpContext http,
        ApplicationDbContext db,
        string? from,
        string? to,
        int? status,
        string? employeeEmail)
    {
        if (!UserCanExport(http)) return Results.Forbid();

        var fromDate = ParseDateOnly(from);
        var toDate   = ParseDateOnly(to);

        var q = db.Timesheets
                  .AsNoTracking()
                  .Include(t => t.Entries)
                  .AsQueryable();

        if (fromDate.HasValue) q = q.Where(t => t.PeriodStart >= fromDate.Value);
        if (toDate.HasValue)   q = q.Where(t => t.PeriodEnd   <= toDate.Value);
        if (status.HasValue)   q = q.Where(t => (int)t.Status == status.Value);

        string? filteredUserId = null;
        if (!string.IsNullOrWhiteSpace(employeeEmail))
        {
            filteredUserId = await db.Users
                .Where(u => u.Email == employeeEmail)
                .Select(u => u.Id)
                .FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(filteredUserId))
            {
                var empty = "TimesheetId,EmployeeEmail,EmployeeName,PeriodStart,PeriodEnd,Status,StatusName,EntryId,WorkDate,Project,Task,Hours,Km,Pieces,HourRate,KmRate,PieceRate,EntryPay,Comment\r\n";
                return Results.File(Encoding.UTF8.GetBytes(empty), "text/csv", "timesheets-details.csv");
            }

            q = q.Where(t => t.EmployeeId == filteredUserId);
        }

        // dotáhnu mapu userId -> (email, name), ať to není N+1
        var userIds = await q.Select(t => t.EmployeeId!).Distinct().ToListAsync();
        var users = await db.Users
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.Email, u.FullName })
            .ToDictionaryAsync(u => u.Id, u => (Email: u.Email ?? "", Name: u.FullName ?? ""));

        var timesheets = await q.OrderByDescending(t => t.PeriodStart).ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("TimesheetId,EmployeeEmail,EmployeeName,PeriodStart,PeriodEnd,Status,StatusName,EntryId,WorkDate,Project,Task,Hours,Km,Pieces,HourRate,KmRate,PieceRate,EntryPay,Comment");

        foreach (var t in timesheets)
        {
            var (email, name) = users.TryGetValue(t.EmployeeId ?? "", out var val)
                ? val
                : ("", "");

            foreach (var e in t.Entries.OrderBy(x => x.WorkDate))
            {
                sb.Append(t.Id).Append(',');
                sb.Append(Escape(email)).Append(',');
                sb.Append(Escape(name)).Append(',');
                sb.Append(FormatDateOnly(t.PeriodStart)).Append(',');
                sb.Append(FormatDateOnly(t.PeriodEnd)).Append(',');
                sb.Append(((int)t.Status)).Append(',');
                sb.Append(Escape(t.Status.ToString())).Append(',');
                sb.Append(e.Id).Append(',');
                sb.Append(FormatDateOnly(e.WorkDate)).Append(',');
                sb.Append(Escape(e.Project)).Append(',');
                sb.Append(Escape(e.Task)).Append(',');
                sb.Append(FormatDecimal(e.Hours)).Append(',');
                sb.Append(FormatDecimal(e.Km)).Append(',');
                sb.Append(e.Pieces).Append(',');
                sb.Append(FormatDecimal(e.HourRate)).Append(',');
                sb.Append(FormatDecimal(e.KmRate)).Append(',');
                sb.Append(FormatDecimal(e.PieceRate)).Append(',');
                sb.Append(FormatDecimal(e.EntryPay)).Append(',');
                sb.AppendLine(Escape(e.Comment));
            }
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return Results.File(bytes, "text/csv", "timesheets-details.csv");
    }

    // === helpers ===

    private static DateOnly? ParseDateOnly(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        return DateOnly.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
            ? d
            : null;
    }

    private static string FormatDateOnly(DateOnly d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string FormatDateTime(DateTime? dt)
        => dt.HasValue ? dt.Value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) : "";

    private static string FormatDecimal(decimal value)
        => value.ToString(CultureInfo.InvariantCulture);

    private static string Escape(string? s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        var needsQuotes = s.Contains(',') || s.Contains('"') || s.Contains('\n') || s.Contains('\r');
        var v = s.Replace("\"", "\"\"");
        return needsQuotes ? $"\"{v}\"" : v;
    }
}