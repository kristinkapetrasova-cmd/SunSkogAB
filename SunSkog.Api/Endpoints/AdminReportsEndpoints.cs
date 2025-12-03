using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SunSkog.Api.Data;

namespace SunSkog.Api.Endpoints;

public static class AdminReportsEndpoints
{
    public static IEndpointRouteBuilder MapAdminReportsEndpoints(this IEndpointRouteBuilder app)
    {
        var grp = app.MapGroup("/api/admin/reports")
            .WithTags("Admin - Reports")
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Admin,Accountant,Management" });

        // Souhrn za období (firma)
        grp.MapGet("/summary", async (ApplicationDbContext db, string? from, string? to) =>
        {
            var (dFrom, dTo) = GetRangeOrDefault(from, to);

            var entriesQ = db.TimesheetEntries
                .AsNoTracking()
                .Where(e => e.WorkDate >= dFrom && e.WorkDate <= dTo);

            var totals = await entriesQ
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Hours  = g.Sum(x => x.Hours),
                    Km     = g.Sum(x => x.Km),
                    Pieces = g.Sum(x => x.Pieces),
                    Pay    = g.Sum(x => x.EntryPay)
                })
                .FirstOrDefaultAsync() ?? new { Hours = 0m, Km = 0m, Pieces = 0, Pay = 0m };

            var tsInRange = await db.TimesheetEntries
                .AsNoTracking()
                .Where(e => e.WorkDate >= dFrom && e.WorkDate <= dTo)
                .Select(e => e.TimesheetId)
                .Distinct()
                .ToListAsync();

            var tsStats = await db.Timesheets
                .AsNoTracking()
                .Where(t => tsInRange.Contains(t.Id))
                .GroupBy(t => t.Status)
                .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
                .ToListAsync();

            var result = new
            {
                currency = "SEK",
                range = new { from = dFrom, to = dTo },
                totals = new
                {
                    hours = totals.Hours,
                    km = totals.Km,
                    pieces = totals.Pieces,
                    pay = totals.Pay
                },
                timesheetsByStatus = tsStats
            };

            return Results.Ok(result);
        })
        .WithOpenApi();

        // Teams – agregace po uživatelích (team zatím z aktivního členství; ne-li, "N/A")
        grp.MapGet("/teams", async (ApplicationDbContext db, string? from, string? to) =>
        {
            var (dFrom, dTo) = GetRangeOrDefault(from, to);

            var q = from e in db.TimesheetEntries.AsNoTracking()
                    join t in db.Timesheets.AsNoTracking() on e.TimesheetId equals t.Id
                    where e.WorkDate >= dFrom && e.WorkDate <= dTo
                    select new
                    {
                        t.EmployeeId,
                        e.Hours,
                        e.Km,
                        e.Pieces,
                        e.EntryPay
                    };

            var byEmployee = await q
                .GroupBy(x => x.EmployeeId)
                .Select(g => new
                {
                    userId = g.Key,
                    hours = g.Sum(x => x.Hours),
                    km = g.Sum(x => x.Km),
                    pieces = g.Sum(x => x.Pieces),
                    pay = g.Sum(x => x.EntryPay)
                })
                .ToListAsync();

            var userIds = byEmployee.Select(x => x.userId).ToList();

            var users = await db.Users
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new { u.Id, u.Email, u.FullName, u.UserName })
                .ToListAsync();

            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var activeMemberships = await db.TeamMemberships
                .Include(m => m.Team)
                .Where(m => userIds.Contains(m.UserId)
                            && m.FromDate <= today
                            && (m.ToDate == null || m.ToDate >= today))
                .ToListAsync();

            var byTeam = byEmployee
                .Select(x =>
                {
                    var u = users.FirstOrDefault(z => z.Id == x.userId);
                    var m = activeMemberships.FirstOrDefault(z => z.UserId == x.userId);
                    return new
                    {
                        team = m?.Team?.Name ?? "N/A",
                        userId = x.userId,
                        email = u?.Email ?? "",
                        name = u?.FullName ?? u?.UserName ?? "",
                        x.hours,
                        x.km,
                        x.pieces,
                        x.pay,
                        currency = "SEK"
                    };
                })
                .OrderByDescending(x => x.pay)
                .ToList();

            var result = new
            {
                range = new { from = dFrom, to = dTo },
                items = byTeam
            };

            return Results.Ok(result);
        })
        .WithOpenApi();

        // NEW: Users – agregace čistě po uživateli (včetně názvu aktivního týmu)
        grp.MapGet("/users", async (ApplicationDbContext db, string? from, string? to) =>
        {
            var (dFrom, dTo) = GetRangeOrDefault(from, to);

            var agg = await (from e in db.TimesheetEntries.AsNoTracking()
                             join t in db.Timesheets.AsNoTracking() on e.TimesheetId equals t.Id
                             where e.WorkDate >= dFrom && e.WorkDate <= dTo
                             group e by t.EmployeeId into g
                             select new
                             {
                                 userId = g.Key,
                                 hours = g.Sum(x => x.Hours),
                                 km = g.Sum(x => x.Km),
                                 pieces = g.Sum(x => x.Pieces),
                                 pay = g.Sum(x => x.EntryPay)
                             })
                .ToListAsync();

            var userIds = agg.Select(a => a.userId).ToList();

            var users = await db.Users
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new { u.Id, u.Email, u.FullName, u.UserName })
                .ToListAsync();

            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var memberships = await db.TeamMemberships
                .Include(m => m.Team)
                .Where(m => userIds.Contains(m.UserId)
                            && m.FromDate <= today
                            && (m.ToDate == null || m.ToDate >= today))
                .ToListAsync();

            var items = agg
                .Select(a =>
                {
                    var u = users.FirstOrDefault(z => z.Id == a.userId);
                    var m = memberships.FirstOrDefault(z => z.UserId == a.userId);
                    return new
                    {
                        userId = a.userId,
                        email = u?.Email ?? "",
                        name = u?.FullName ?? u?.UserName ?? "",
                        team = m?.Team?.Name ?? "N/A",
                        a.hours,
                        a.km,
                        a.pieces,
                        a.pay,
                        currency = "SEK"
                    };
                })
                .OrderByDescending(x => x.pay)
                .ToList();

            var result = new
            {
                range = new { from = dFrom, to = dTo },
                items
            };

            return Results.Ok(result);
        })
        .WithOpenApi();

        return app;
    }

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
}