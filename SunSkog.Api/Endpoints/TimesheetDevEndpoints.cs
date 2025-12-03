using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using SunSkog.Api.Data;
using SunSkog.Api.Storage.Entities;

namespace SunSkog.Api.Endpoints;

public static class TimesheetDevEndpoints
{
    public static IEndpointRouteBuilder MapTimesheetDevEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/dev/timesheets")
                     .WithTags("Dev")
                     .RequireAuthorization();

        // POST /dev/timesheets/create-sample  → vytvoří ukázkový výkaz (aktuální user)
        api.MapPost("/create-sample", async (HttpContext http, ApplicationDbContext db) =>
        {
            var userId = GetUserId(http.User);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            // 14denní období končící v neděli
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var periodEnd = today.AddDays((int)DayOfWeek.Sunday - (int)today.DayOfWeek);
            var periodStart = periodEnd.AddDays(-13);

            var t = new Timesheet
            {
                Id = Guid.NewGuid(),
                EmployeeId = userId,
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                Status = TimesheetStatus.Draft,
                Notes = "DEV sample timesheet",
                Entries = new List<TimesheetEntry>()
            };

            // 2 ukázkové položky
            t.Entries.Add(new TimesheetEntry
            {
                Id = Guid.NewGuid(),
                TimesheetId = t.Id,
                WorkDate = periodStart,
                Project = "SE-FOREST-001",
                Task = "Planting",
                Hours = 7.5m,
                Km = 18m,
                Pieces = 120,
                HourRate = 220m,
                KmRate = 6.5m,
                PieceRate = 2.2m,
                EntryPay = 0m,
                Comment = "Morning shift"
            });
            t.Entries.Add(new TimesheetEntry
            {
                Id = Guid.NewGuid(),
                TimesheetId = t.Id,
                WorkDate = periodStart.AddDays(1),
                Project = "SE-FOREST-001",
                Task = "Clearing",
                Hours = 6m,
                Km = 12m,
                Pieces = 0,
                HourRate = 220m,
                KmRate = 6.5m,
                PieceRate = 0m,
                EntryPay = 0m,
                Comment = "Afternoon shift"
            });

            // přepočet součtů
            RecalculateTotals(t);

            db.Timesheets.Add(t);
            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                t.Id,
                t.EmployeeId,
                t.PeriodStart,
                t.PeriodEnd,
                Status = t.Status.ToString(),
                t.TotalHours,
                t.TotalKm,
                t.TotalPieces,
                t.TotalPay
            });
        })
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .WithOpenApi();

        // GET /dev/timesheets/my-last → rychlé získání posledního výkazu přihlášeného usera
        api.MapGet("/my-last", async (HttpContext http, ApplicationDbContext db) =>
        {
            var userId = GetUserId(http.User);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            var t = await db.Timesheets
                .AsNoTracking()
                .Where(x => x.EmployeeId == userId)
                .OrderByDescending(x => x.PeriodStart)
                .Select(x => new
                {
                    x.Id,
                    x.EmployeeId,
                    x.PeriodStart,
                    x.PeriodEnd,
                    Status = x.Status.ToString(),
                    x.TotalHours,
                    x.TotalKm,
                    x.TotalPieces,
                    x.TotalPay
                })
                .FirstOrDefaultAsync();

            return t is null ? Results.NotFound() : Results.Ok(t);
        })
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status404NotFound)
        .WithOpenApi();

        return app;
    }

    private static void RecalculateTotals(Timesheet t)
    {
        decimal totalHours = 0, totalKm = 0, totalPay = 0;
        int totalPieces = 0;

        foreach (var e in t.Entries)
        {
            var pay = (e.Hours * e.HourRate) + (e.Km * e.KmRate) + (e.Pieces * e.PieceRate);
            e.EntryPay = Math.Round(pay, 2, MidpointRounding.AwayFromZero);

            totalHours += e.Hours;
            totalKm += e.Km;
            totalPieces += e.Pieces;
            totalPay += e.EntryPay;
        }

        t.TotalHours = Math.Round(totalHours, 2, MidpointRounding.AwayFromZero);
        t.TotalKm = Math.Round(totalKm, 2, MidpointRounding.AwayFromZero);
        t.TotalPieces = totalPieces;
        t.TotalPay = Math.Round(totalPay, 2, MidpointRounding.AwayFromZero);
    }

    private static string? GetUserId(ClaimsPrincipal user)
        => user.FindFirstValue(ClaimTypes.NameIdentifier)
           ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub)
           ?? user.FindFirstValue("sub");
}