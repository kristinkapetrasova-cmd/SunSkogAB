using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SunSkog.Api.Data;
using SunSkog.Api.Models;

namespace SunSkog.Api.Endpoints;

public static class AdminTimesheetEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        // /api/admin/timesheets – pouze pro Manager/SuperAdmin
        var g = app.MapGroup("/api/admin/timesheets").RequireAuthorization();

        // GET /api/admin/timesheets?from=YYYY-MM-DD&to=YYYY-MM-DD&status=0..3&employeeEmail=...&page=1&pageSize=20
        g.MapGet("", async (
            HttpContext http,
            ApplicationDbContext db,
            string? from,
            string? to,
            int? status,
            string? employeeEmail,
            int page = 1,
            int pageSize = 20
        ) =>
        {
            if (!UserCanApprove(http)) return Results.Forbid();

            // vstupní parametry
            var fromDate = ParseDateOnly(from);
            var toDate   = ParseDateOnly(to);
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 1;
            if (pageSize > 200) pageSize = 200;

            // základní dotaz
            var q = db.Timesheets.AsNoTracking();

            if (fromDate.HasValue) q = q.Where(t => t.PeriodStart >= fromDate.Value);
            if (toDate.HasValue)   q = q.Where(t => t.PeriodEnd   <= toDate.Value);
            if (status.HasValue)   q = q.Where(t => (int)t.Status == status.Value);

            if (!string.IsNullOrWhiteSpace(employeeEmail))
            {
                // najdu uživatele podle e-mailu
                var uid = await db.Users
                    .Where(u => u.Email == employeeEmail)
                    .Select(u => u.Id)
                    .FirstOrDefaultAsync();

                if (string.IsNullOrEmpty(uid))
                {
                    // nikdo takový – vrátíme prázdnou stránku
                    var empty = new Paged<AdminTimesheetListItemDto>(page, pageSize, 0, new List<AdminTimesheetListItemDto>());
                    return Results.Ok(empty);
                }

                q = q.Where(t => t.EmployeeId == uid);
            }

            var total = await q.CountAsync();
            var skip  = (page - 1) * pageSize;

            // join na Users kvůli e-mailu a jménu
            var pageItems = await (
                from t in q
                join u in db.Users on t.EmployeeId equals u.Id into gj
                from u in gj.DefaultIfEmpty()
                orderby t.PeriodStart descending
                select new AdminTimesheetListItemDto(
                    t.Id,
                    u != null ? (u.Email ?? "") : "",
                    u != null ? (u.FullName ?? "") : "",
                    t.PeriodStart,
                    t.PeriodEnd,
                    (int)t.Status,
                    t.SubmittedAt,
                    t.ApprovedAt,
                    t.TotalHours,
                    t.TotalKm,
                    t.TotalPieces,
                    t.TotalPay
                )
            )
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync();

            var result = new Paged<AdminTimesheetListItemDto>(page, pageSize, total, pageItems);
            return Results.Ok(result);
        });
    }

    private static bool UserCanApprove(HttpContext http)
        => http.User.IsInRole("Manager") || http.User.IsInRole("SuperAdmin");

    private static DateOnly? ParseDateOnly(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        return DateOnly.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
            ? d
            : null;
    }

    // jednoduché DTO pro seznam
    public sealed record AdminTimesheetListItemDto(
        Guid Id,
        string EmployeeEmail,
        string EmployeeName,
        DateOnly PeriodStart,
        DateOnly PeriodEnd,
        int Status,
        DateTime? SubmittedAt,
        DateTime? ApprovedAt,
        decimal TotalHours,
        decimal TotalKm,
        int TotalPieces,
        decimal TotalPay
    );

    public sealed record Paged<T>(
        int Page,
        int PageSize,
        int Total,
        IReadOnlyList<T> Items
    );
}