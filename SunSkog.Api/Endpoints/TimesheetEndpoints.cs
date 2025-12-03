using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SunSkog.Api.Data;
using SunSkog.Api.Models;
using SunSkog.Api.Storage.Entities;

namespace SunSkog.Api.Endpoints;

public static class TimesheetEndpoints
{
    public static IEndpointRouteBuilder MapTimesheetEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app
            .MapGroup("/api/timesheets")
            .WithTags("Timesheets")
            .RequireAuthorization();

        // --- Debug ping (ověření auth + routingu)
        g.MapGet("/debug/ping", (ClaimsPrincipal user) =>
        {
            var uid = GetUserId(user) ?? "(none)";
            var roles = user.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
            return Results.Ok(new { ok = true, userId = uid, roles });
        });

        // --- GET: seznam výkazů (podle role)
        // Worker: pouze vlastní
        // TeamLead/Accountant/Management/Admin: všechny
        // Podporuje filtry: ?from=yyyy-MM-dd&to=yyyy-MM-dd&status=0-3
        g.MapGet("/", async (
            ApplicationDbContext db, 
            ClaimsPrincipal user, 
            UserManager<ApplicationUser> userManager,
            HttpContext http) =>
        {
            var userId = GetUserId(user);
            if (userId is null) return Results.Unauthorized();

            // Zjistit role uživatele
            var canViewAll = user.IsInRole("TeamLead") || 
                             user.IsInRole("Accountant") || 
                             user.IsInRole("Management") || 
                             user.IsInRole("Admin") ||
                             user.IsInRole("Manager") ||
                             user.IsInRole("SuperAdmin");

            IQueryable<Timesheet> query = db.Timesheets.AsNoTracking();

            if (!canViewAll)
            {
                // Worker vidí pouze své výkazy
                query = query.Where(t => t.EmployeeId == userId);
            }

            // FILTROVÁNÍ podle query parametrů
            var fromStr = http.Request.Query["from"].ToString();
            var toStr = http.Request.Query["to"].ToString();
            var statusStr = http.Request.Query["status"].ToString();

            // Filtr: od data
            if (!string.IsNullOrWhiteSpace(fromStr) && TryParseDateOnly(fromStr, out var fromDate))
            {
                query = query.Where(t => t.PeriodStart >= fromDate);
            }

            // Filtr: do data
            if (!string.IsNullOrWhiteSpace(toStr) && TryParseDateOnly(toStr, out var toDate))
            {
                query = query.Where(t => t.PeriodEnd <= toDate);
            }

            // Filtr: status (0=Draft, 1=Submitted, 2=Approved, 3=Returned)
            if (!string.IsNullOrWhiteSpace(statusStr) && int.TryParse(statusStr, out var statusInt))
            {
                var status = (TimesheetStatus)statusInt;
                query = query.Where(t => t.Status == status);
            }

            // Načíst uživatele pro jména
            var userIds = await query.Select(t => t.EmployeeId).Distinct().ToListAsync();
            var users = await userManager.Users
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new { u.Id, u.UserName, u.Email, Name = u.FullName ?? u.UserName })
                .ToDictionaryAsync(u => u.Id);

            var list = await query
                .OrderByDescending(t => t.PeriodStart)
                .Select(t => new
                {
                    t.Id,
                    t.EmployeeId,
                    UserId = t.EmployeeId,
                    t.PeriodStart,
                    t.PeriodEnd,
                    Status = t.Status.ToString(),
                    t.TotalHours,
                    t.TotalKm,
                    t.TotalPieces,
                    t.TotalPay,
                    t.SubmittedAt,
                    t.ApprovedAt
                })
                .ToListAsync();

            // Přidat jména uživatelů
            var result = list.Select(t => new
            {
                t.Id,
                t.EmployeeId,
                t.UserId,
                UserName = users.TryGetValue(t.EmployeeId, out var u) ? u.Name : null,
                UserEmail = users.TryGetValue(t.EmployeeId, out var ue) ? ue.Email : null,
                t.PeriodStart,
                t.PeriodEnd,
                t.Status,
                t.TotalHours,
                t.TotalKm,
                t.TotalPieces,
                t.TotalPay,
                t.SubmittedAt,
                t.ApprovedAt
            });

            return Results.Ok(result);
        });

        // --- GET: detail (vlastní nebo pro adminy všechny)
        g.MapGet("/{id:guid}", async (Guid id, ApplicationDbContext db, ClaimsPrincipal user) =>
        {
            var userId = GetUserId(user);
            if (userId is null) return Results.Unauthorized();

            var canViewAll = user.IsInRole("TeamLead") || 
                             user.IsInRole("Accountant") || 
                             user.IsInRole("Management") || 
                             user.IsInRole("Admin") ||
                             user.IsInRole("Manager") ||
                             user.IsInRole("SuperAdmin");

            var ts = await db.Timesheets
                .AsNoTracking()
                .Include(t => t.Entries)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (ts is null)
                return Results.NotFound(new { error = "Výkaz nenalezen." });

            // Kontrola přístupu
            if (!canViewAll && ts.EmployeeId != userId)
                return Results.Forbid();

            return Results.Ok(new
            {
                ts.Id,
                ts.EmployeeId,
                UserId = ts.EmployeeId,
                ts.PeriodStart,
                ts.PeriodEnd,
                Status = ts.Status.ToString(),
                ts.TotalHours,
                ts.TotalKm,
                ts.TotalPieces,
                ts.TotalPay,
                ts.SubmittedAt,
                ts.ApprovedAt,
                Entries = ts.Entries
                    .OrderBy(e => e.WorkDate)
                    .Select(e => new
                    {
                        e.Id,
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
                    })
                    .ToList()
            });
        });

        // --- POST: vytvoření nového výkazu
        g.MapPost("/", async (
            HttpRequest http,
            ApplicationDbContext db,
            ClaimsPrincipal user,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("TimesheetEndpoints.Create");

            try
            {
                var userId = GetUserId(user);
                if (userId is null) return Results.Unauthorized();

                // Management a Accountant nemohou vytvářet výkazy
                if (user.IsInRole("Management") || user.IsInRole("Accountant"))
                {
                    if (!user.IsInRole("Admin") && !user.IsInRole("SuperAdmin"))
                    {
                        return Results.Forbid();
                    }
                }

                string raw;
                using (var reader = new StreamReader(http.Body))
                    raw = await reader.ReadToEndAsync();

                if (string.IsNullOrWhiteSpace(raw))
                    return Results.BadRequest(new { error = "Prázdné tělo požadavku." });

                using var doc = JsonDocument.Parse(raw);
                var root = doc.RootElement;

                var startStr = root.TryGetProperty("periodStart", out var ps)
                    ? ps.GetString() : root.TryGetProperty("PeriodStart", out var Ps) ? Ps.GetString() : null;

                var endStr = root.TryGetProperty("periodEnd", out var pe)
                    ? pe.GetString() : root.TryGetProperty("PeriodEnd", out var Pe) ? Pe.GetString() : null;

                var notes = root.TryGetProperty("notes", out var n)
                    ? n.GetString() : root.TryGetProperty("Notes", out var N) ? N.GetString() : null;

                if (!TryParseDateOnly(startStr, out var start))
                    return Results.BadRequest(new { error = "Neplatný formát 'periodStart'. Očekává se 'yyyy-MM-dd'." });

                if (!TryParseDateOnly(endStr, out var end))
                    return Results.BadRequest(new { error = "Neplatný formát 'periodEnd'. Očekává se 'yyyy-MM-dd'." });

                if (end < start)
                    return Results.BadRequest(new { error = "Konec období nesmí být dříve než začátek." });

                var entity = new Timesheet
                {
                    Id = Guid.NewGuid(),
                    EmployeeId = userId,
                    PeriodStart = start,
                    PeriodEnd = end,
                    Notes = string.IsNullOrWhiteSpace(notes) ? null : notes,
                    Status = TimesheetStatus.Draft,
                    TotalHours = 0m,
                    TotalKm = 0m,
                    TotalPieces = 0,
                    TotalPay = 0m,
                    SubmittedAt = null,
                    ApprovedAt = null,
                    Entries = new List<TimesheetEntry>()
                };

                db.Timesheets.Add(entity);
                await db.SaveChangesAsync();

                return Results.Created($"/api/timesheets/{entity.Id}", new { entity.Id });
            }
            catch (DbUpdateException ex)
            {
                logger.LogError(ex, "DB chyba při vytváření výkazu");
                var detail = ex.InnerException?.Message ?? ex.Message;
                return Results.Problem(title: "Chyba databáze při vytváření výkazu.", detail: detail, statusCode: 500);
            }
            catch (JsonException ex)
            {
                logger.LogError(ex, "Neplatný JSON");
                return Results.BadRequest(new { error = "Neplatný JSON v těle požadavku.", detail = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Nečekaná chyba při vytváření výkazu");
                return Results.Problem(title: "Nečekaná chyba při vytváření výkazu.", detail: ex.Message, statusCode: 500);
            }
        });

        return app;
    }

    private static string? GetUserId(ClaimsPrincipal user)
        => user.FindFirstValue(ClaimTypes.NameIdentifier)
           ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);

    private static bool TryParseDateOnly(string? value, out DateOnly result)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            if (DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
                return true;

            if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dt))
            {
                result = DateOnly.FromDateTime(dt);
                return true;
            }
        }

        result = default;
        return false;
    }
}
