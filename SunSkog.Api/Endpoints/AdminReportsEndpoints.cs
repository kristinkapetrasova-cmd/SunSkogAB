using Microsoft.EntityFrameworkCore;
using SunSkog.Api.Contracts;
using SunSkog.Api.Data;
using SunSkog.Api.Models.Domain;

namespace SunSkog.Api.Endpoints;

public static class AdminReportsEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/admin/reports").RequireAuthorization();

        // GET /api/admin/reports/summary/employees?from=&to=&status=
        g.MapGet("/summary/employees", async (
            ApplicationDbContext db,
            string? from,
            string? to,
            int? status
        ) =>
        {
            DateOnly? fromDate = null, toDate = null;
            if (!string.IsNullOrWhiteSpace(from) && DateOnly.TryParse(from, out var f)) fromDate = f;
            if (!string.IsNullOrWhiteSpace(to)   && DateOnly.TryParse(to,   out var tParsed)) toDate = tParsed;

            IQueryable<Models.Domain.Timesheet> q = db.Timesheets.AsNoTracking();

            if (fromDate.HasValue) q = q.Where(ts => ts.PeriodEnd   >= fromDate.Value);
            if (toDate.HasValue)   q = q.Where(ts => ts.PeriodStart <= toDate.Value);

            if (status.HasValue)
            {
                var wanted = (TimesheetStatus)status.Value;
                q = q.Where(ts => ts.Status == wanted);
            }

            var data = await q
                .GroupBy(ts => ts.EmployeeId)
                .Select(g1 => new
                {
                    EmployeeId    = g1.Key,
                    TotalTs       = g1.Count(),
                    DraftCount    = g1.Count(x => x.Status == TimesheetStatus.Draft),
                    SubmittedCnt  = g1.Count(x => x.Status == TimesheetStatus.Submitted),
                    ApprovedCnt   = g1.Count(x => x.Status == TimesheetStatus.Approved),
                    ReturnedCnt   = g1.Count(x => x.Status == TimesheetStatus.Returned),
                    Hours         = g1.Sum(x => x.TotalHours),
                    Km            = g1.Sum(x => x.TotalKm),
                    Pieces        = g1.Sum(x => x.TotalPieces),
                    Pay           = g1.Sum(x => x.TotalPay)
                })
                .ToListAsync();

            var userIds = data.Select(d => d.EmployeeId).ToList();

            var users = await db.Users.AsNoTracking()
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new { u.Id, u.Email, u.FullName })
                .ToListAsync();

            var res = data
                .Select(d =>
                {
                    var u = users.FirstOrDefault(x => x.Id == d.EmployeeId);
                    return new AdminEmployeeSummaryDto
                    {
                        EmployeeEmail   = u?.Email,
                        EmployeeName    = u?.FullName,
                        TotalTimesheets = d.TotalTs,
                        DraftCount      = d.DraftCount,
                        SubmittedCount  = d.SubmittedCnt,
                        ApprovedCount   = d.ApprovedCnt,
                        ReturnedCount   = d.ReturnedCnt,
                        TotalHours      = d.Hours,
                        TotalKm         = d.Km,
                        TotalPieces     = d.Pieces,
                        TotalPay        = d.Pay
                    };
                })
                .OrderBy(r => r.EmployeeName ?? r.EmployeeEmail)
                .ToList();

            return Results.Ok(res);
        });

        // GET /api/admin/reports/summary/daily?from=&to=&status=&employeeEmail=
        g.MapGet("/summary/daily", async (
            ApplicationDbContext db,
            string? from,
            string? to,
            int? status,
            string? employeeEmail
        ) =>
        {
            DateOnly? fromDate = null, toDate = null;
            if (!string.IsNullOrWhiteSpace(from) && DateOnly.TryParse(from, out var f)) fromDate = f;
            if (!string.IsNullOrWhiteSpace(to)   && DateOnly.TryParse(to,   out var tParsed)) toDate = tParsed;

            // Start: entry + timesheet
            var joined = db.TimesheetEntries.AsNoTracking()
                .Join(
                    db.Timesheets.AsNoTracking(),
                    e  => e.TimesheetId,
                    ts => ts.Id,
                    (e, ts) => new { e, ts }
                );

            if (fromDate.HasValue) joined = joined.Where(x => x.e.WorkDate >= fromDate.Value);
            if (toDate.HasValue)   joined = joined.Where(x => x.e.WorkDate <= toDate.Value);

            if (status.HasValue)
            {
                var wanted = (TimesheetStatus)status.Value;
                joined = joined.Where(x => x.ts.Status == wanted);
            }

            if (!string.IsNullOrWhiteSpace(employeeEmail))
            {
                joined = joined
                    .Join(
                        db.Users.AsNoTracking(),
                        x => x.ts.EmployeeId,
                        u => u.Id,
                        (x, u) => new { x.e, x.ts, u }
                    )
                    .Where(x => x.u.Email != null && x.u.Email == employeeEmail)
                    .Select(x => new { e = x.e, ts = x.ts });
            }

            var res = await joined
                .GroupBy(x => x.e.WorkDate)
                .Select(g1 => new AdminDailyTotalsDto
                {
                    Date        = g1.Key,
                    TotalHours  = g1.Sum(v => v.e.Hours),
                    TotalKm     = g1.Sum(v => v.e.Km),
                    TotalPieces = g1.Sum(v => v.e.Pieces),
                    TotalPay    = g1.Sum(v => v.e.EntryPay)
                })
                .OrderBy(r => r.Date)
                .ToListAsync();

            return Results.Ok(res);
        });
    }
}