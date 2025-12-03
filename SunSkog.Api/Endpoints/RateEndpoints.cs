using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SunSkog.Api.Data;
using SunSkog.Api.Models;
using SunSkog.Api.Storage.Entities;
using System.Security.Claims;

namespace SunSkog.Api.Endpoints;

public static class RateEndpoints
{
    public static WebApplication MapRateEndpoints(this WebApplication app)
    {
        // GET /api/rates/current - aktuální platné sazby
        app.MapGet("/api/rates/current", [Authorize] async (ApplicationDbContext db) =>
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            
            var currentRate = await db.Rates
                .Where(r => r.ValidFrom <= today && (r.ValidTo == null || r.ValidTo >= today))
                .OrderByDescending(r => r.ValidFrom)
                .FirstOrDefaultAsync();

            if (currentRate == null)
            {
                // Vrátit výchozí hodnoty pokud nejsou žádné sazby
                return Results.Ok(new
                {
                    hourRate = 150m,
                    kmRate = 2.5m,
                    pieceRate = 10m,
                    validFrom = today,
                    validTo = (DateOnly?)null
                });
            }

            return Results.Ok(new
            {
                hourRate = currentRate.HourRate,
                kmRate = currentRate.KmRate,
                pieceRate = currentRate.PieceRate,
                validFrom = currentRate.ValidFrom,
                validTo = currentRate.ValidTo
            });
        })
        .WithTags("Rates")
        .WithOpenApi();

        // GET /api/rates/history - historie sazeb (pouze Management/Admin)
        app.MapGet("/api/rates/history", [Authorize] async (
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            ClaimsPrincipal currentUser
        ) =>
        {
            // Kontrola oprávnění
            if (!currentUser.IsInRole("Management") && !currentUser.IsInRole("Admin"))
            {
                return Results.Forbid();
            }

            var rates = await db.Rates
                .OrderByDescending(r => r.ValidFrom)
                .ToListAsync();

            // Načíst jména uživatelů kteří změnili sazby
            var userIds = rates
                .Where(r => r.ChangedByUserId != null)
                .Select(r => r.ChangedByUserId!)
                .Distinct()
                .ToList();

            var users = await userManager.Users
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.FullName ?? u.UserName ?? u.Email);

            var result = rates.Select(r => new
            {
                id = r.Id,
                hourRate = r.HourRate,
                kmRate = r.KmRate,
                pieceRate = r.PieceRate,
                validFrom = r.ValidFrom,
                validTo = r.ValidTo,
                createdAt = r.CreatedAt,
                changedByUserId = r.ChangedByUserId,
                changedByUserName = r.ChangedByUserId != null && users.ContainsKey(r.ChangedByUserId) 
                    ? users[r.ChangedByUserId] 
                    : null
            });

            return Results.Ok(result);
        })
        .WithTags("Rates")
        .WithOpenApi();

        // POST /api/rates - vytvořit novou sazbu (pouze Management)
        app.MapPost("/api/rates", [Authorize(Roles = "Management,Admin")] async (
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            ClaimsPrincipal currentUser,
            CreateRateRequest req
        ) =>
        {
            var userId = userManager.GetUserId(currentUser);
            var today = DateOnly.FromDateTime(DateTime.Today);

            // Ukončit všechny aktuálně platné sazby (nastavit ValidTo na včera)
            var currentRates = await db.Rates
                .Where(r => r.ValidTo == null || r.ValidTo >= today)
                .ToListAsync();

            var yesterday = today.AddDays(-1);
            foreach (var rate in currentRates)
            {
                if (rate.ValidTo == null || rate.ValidTo >= today)
                {
                    rate.ValidTo = yesterday;
                }
            }

            // Vytvořit novou sazbu platnou od dneška
            var newRate = new Rate
            {
                Id = Guid.NewGuid(),
                HourRate = req.HourRate,
                KmRate = req.KmRate,
                PieceRate = req.PieceRate,
                ValidFrom = req.ValidFrom ?? today,
                ValidTo = null, // platí do odvolání
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                ChangedByUserId = userId
            };

            db.Rates.Add(newRate);
            await db.SaveChangesAsync();

            return Results.Created($"/api/rates/{newRate.Id}", new
            {
                id = newRate.Id,
                hourRate = newRate.HourRate,
                kmRate = newRate.KmRate,
                pieceRate = newRate.PieceRate,
                validFrom = newRate.ValidFrom,
                validTo = newRate.ValidTo
            });
        })
        .WithTags("Rates")
        .WithOpenApi();

        return app;
    }

    public record CreateRateRequest(
        decimal HourRate,
        decimal KmRate,
        decimal PieceRate,
        DateOnly? ValidFrom = null
    );
}