using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SunSkog.Api.Contracts;
using SunSkog.Api.Data;
using SunSkog.Api.Models.Domain;

namespace SunSkog.Api.Endpoints;

public static class AdminDashboardEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/admin/dashboard").RequireAuthorization();

        // GET /api/admin/dashboard/summary?from=YYYY-MM-DD&to=YYYY-MM-DD&status=2&employeeEmail=...
        g.MapGet("/summary", async (
            [FromServices] ApplicationDbContext db,
            string? from,
            string? to,
            int? status,
            string? employeeEmail
        ) =>
        {
            // --- filtry ---
            DateOnly? dFrom = TryParseDateOnly(from);
            DateOnly? dTo   = TryParseDateOnly(to);

            IQueryable<Timesheet> q = db.Timesheets.AsNoTracking();

            if (dFrom.HasValue)  q = q.Where(t => t.PeriodStart >= dFrom.Value);
            if (dTo.HasValue)    q = q.Where(t => t.PeriodEnd   <= dTo.Value);
            if (status.HasValue)
            {
                var st = (TimesheetStatus)status.Value;
                q = q.Where(t => t.Status == st);
            }
            if (!string.IsNullOrWhiteSpace(employeeEmail))
            {
                q =
                    from t in q
                    join u in db.Users.AsNoTracking() on t.EmployeeId equals u.Id
                    where u.Email == employeeEmail
                    select t;
            }

            // --- počty dle statusu (jedním dotazem) ---
            var countsRaw = await q
                .GroupBy(t => t.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            int cDraft    = countsRaw.FirstOrDefault(x => x.Status == TimesheetStatus.Draft)?.Count ?? 0;
            int cSubmit   = countsRaw.FirstOrDefault(x => x.Status == TimesheetStatus.Submitted)?.Count ?? 0;
            int cApprove  = countsRaw.FirstOrDefault(x => x.Status == TimesheetStatus.Approved)?.Count ?? 0;
            int cReturn   = countsRaw.FirstOrDefault(x => x.Status == TimesheetStatus.Returned)?.Count ?? 0;

            // --- sumy (v DB) ---
            var totalsRaw = await q
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    TotalHours  = g.Sum(x => x.TotalHours),
                    TotalKm     = g.Sum(x => x.TotalKm),
                    TotalPieces = g.Sum(x => x.TotalPieces),
                    TotalPay    = g.Sum(x => x.TotalPay)
                })
                .FirstOrDefaultAsync();

            var totals = new TotalsDto
            {
                TotalHours  = totalsRaw?.TotalHours  ?? 0,
                TotalKm     = totalsRaw?.TotalKm     ?? 0,
                TotalPieces = totalsRaw?.TotalPieces ?? 0,
                TotalPay    = totalsRaw?.TotalPay    ?? 0
            };

            // --- posledních 10 změn (ApprovedAt/SubmittedAt, jinak null) ---
            var recent = await
                (from t in q
                 join u in db.Users.AsNoTracking() on t.EmployeeId equals u.Id into gj
                 from u in gj.DefaultIfEmpty()
                 let lastChange = (DateTime?)(t.ApprovedAt ?? t.SubmittedAt)
                 orderby lastChange descending, t.PeriodStart descending
                 select new RecentItemDto
                 {
                     Id           = t.Id,
                     EmployeeEmail= u != null ? u.Email    : null,
                     EmployeeName = u != null ? u.FullName : null,
                     PeriodStart  = t.PeriodStart,
                     PeriodEnd    = t.PeriodEnd,
                     Status       = (int)t.Status,
                     LastChange   = lastChange
                 })
                .Take(10)
                .ToListAsync();

            // --- TOP 5 zaměstnanců dle odměny ---
            var topAgg = await
                (from t in q
                 group t by t.EmployeeId into g1
                 select new
                 {
                     EmployeeId = g1.Key,
                     TotalPay   = g1.Sum(x => x.TotalPay),
                     TotalHours = g1.Sum(x => x.TotalHours)
                 })
                .OrderByDescending(x => x.TotalPay)
                .Take(5)
                .ToListAsync();

            var topIds  = topAgg.Select(x => x.EmployeeId).ToList();
            var topUsers = await db.Users.AsNoTracking()
                              .Where(u => topIds.Contains(u.Id))
                              .Select(u => new { u.Id, u.Email, u.FullName })
                              .ToListAsync();

            var topEmployees = topAgg
                .Select(x =>
                {
                    var u = topUsers.FirstOrDefault(z => z.Id == x.EmployeeId);
                    return new TopEmployeeDto
                    {
                        EmployeeEmail = u?.Email,
                        EmployeeName  = u?.FullName,
                        TotalPay      = x.TotalPay,
                        TotalHours    = x.TotalHours
                    };
                })
                .ToList();

            var dto = new AdminDashboardDto
            {
                StatusCounts = new StatusCountsDto
                {
                    Draft     = cDraft,
                    Submitted = cSubmit,
                    Approved  = cApprove,
                    Returned  = cReturn
                },
                Totals       = totals,
                Recent       = recent,
                TopEmployees = topEmployees
            };

            return Results.Ok(dto);
        });
    }

    private static DateOnly? TryParseDateOnly(string? value)
        => DateOnly.TryParse(value, out var d) ? d : (DateOnly?)null;
}