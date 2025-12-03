using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SunSkog.Api.Data;
using SunSkog.Api.Models;
using SunSkog.Api.Storage.Entities;
using System.Security.Claims;
using System.Text.Json;

namespace SunSkog.Api.Endpoints;

public static class TeamEndpoints
{
    public static IEndpointRouteBuilder MapTeamEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app
            .MapGroup("/api/teams")
            .WithTags("Teams")
            .RequireAuthorization(policy => policy.RequireRole("Admin"));

        // GET: seznam týmů
        g.MapGet("/", async (ApplicationDbContext db, UserManager<ApplicationUser> userManager) =>
        {
            var teams = await db.Teams
                .AsNoTracking()
                .OrderBy(t => t.Name)
                .ToListAsync();

            // Načíst všechny aktivní členství najednou
            var teamIds = teams.Select(t => t.Id).ToList();
            var memberships = await db.TeamMemberships
                .AsNoTracking()
                .Where(m => teamIds.Contains(m.TeamId) && m.ToDate == null)
                .GroupBy(m => m.TeamId)
                .Select(g => new { TeamId = g.Key, Count = g.Count() })
                .ToListAsync();

            var result = new List<object>();
            foreach (var team in teams)
            {
                var leadUser = team.LeadUserId != null 
                    ? await userManager.FindByIdAsync(team.LeadUserId)
                    : null;

                var memberCount = memberships.FirstOrDefault(m => m.TeamId == team.Id)?.Count ?? 0;

                result.Add(new
                {
                    team.Id,
                    team.Name,
                    MemberCount = memberCount,
                    LeadId = team.LeadUserId,
                    LeadName = leadUser?.FullName ?? leadUser?.UserName
                });
            }

            return Results.Ok(result);
        });

        // GET: detail týmu
        g.MapGet("/{id:guid}", async (Guid id, ApplicationDbContext db, UserManager<ApplicationUser> userManager) =>
        {
            var team = await db.Teams
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == id);

            if (team is null)
                return Results.NotFound(new { error = "Tým nenalezen." });

            // Načíst členy přímo z TeamMemberships tabulky
            var memberships = await db.TeamMemberships
                .AsNoTracking()
                .Where(m => m.TeamId == id && m.ToDate == null)
                .ToListAsync();

            var userIds = memberships.Select(m => m.UserId).Distinct().ToList();
            var users = await userManager.Users
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new { u.Id, u.UserName, u.Email, Name = u.FullName ?? u.UserName })
                .ToListAsync();

            var members = memberships.Select(m =>
            {
                var user = users.FirstOrDefault(u => u.Id == m.UserId);
                return new
                {
                    UserId = m.UserId,
                    UserName = user?.Name ?? user?.UserName ?? "Unknown",
                    UserEmail = user?.Email,
                    IsLead = m.UserId == team.LeadUserId,
                    JoinedAt = m.FromDate
                };
            }).ToList();

            return Results.Ok(new
            {
                team.Id,
                team.Name,
                Members = members
            });
        });

        // POST: vytvoření týmu
        g.MapPost("/", async (HttpRequest http, ApplicationDbContext db) =>
        {
            try
            {
                string raw;
                using (var reader = new StreamReader(http.Body))
                    raw = await reader.ReadToEndAsync();

                using var doc = JsonDocument.Parse(raw);
                var root = doc.RootElement;

                var name = root.TryGetProperty("name", out var n) ? n.GetString() : null;

                if (string.IsNullOrWhiteSpace(name))
                    return Results.BadRequest(new { error = "Název týmu je povinný." });

                var team = new Team
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    LeadUserId = null,
                    Members = new List<TeamMembership>()
                };

                db.Teams.Add(team);
                await db.SaveChangesAsync();

                return Results.Created($"/api/teams/{team.Id}", new { team.Id });
            }
            catch (Exception ex)
            {
                return Results.Problem(title: "Chyba při vytváření týmu.", detail: ex.Message, statusCode: 500);
            }
        });

        // PUT: úprava týmu
        g.MapPut("/{id:guid}", async (Guid id, HttpRequest http, ApplicationDbContext db) =>
        {
            try
            {
                var team = await db.Teams.FirstOrDefaultAsync(t => t.Id == id);
                if (team is null)
                    return Results.NotFound(new { error = "Tým nenalezen." });

                string raw;
                using (var reader = new StreamReader(http.Body))
                    raw = await reader.ReadToEndAsync();

                using var doc = JsonDocument.Parse(raw);
                var root = doc.RootElement;

                if (root.TryGetProperty("name", out var n) && n.GetString() is string name)
                    team.Name = name;

                await db.SaveChangesAsync();

                return Results.NoContent();
            }
            catch (Exception ex)
            {
                return Results.Problem(title: "Chyba při úpravě týmu.", detail: ex.Message, statusCode: 500);
            }
        });

        // DELETE: smazání týmu
        g.MapDelete("/{id:guid}", async (Guid id, ApplicationDbContext db) =>
        {
            var team = await db.Teams
                .Include(t => t.Members)
                .FirstOrDefaultAsync(t => t.Id == id);
                
            if (team is null)
                return Results.NotFound(new { error = "Tým nenalezen." });

            // Smazat všechny členství
            db.TeamMemberships.RemoveRange(team.Members);

            db.Teams.Remove(team);
            await db.SaveChangesAsync();

            return Results.NoContent();
        });

        // POST: přidat člena do týmu
        g.MapPost("/{id:guid}/members", async (Guid id, HttpRequest http, ApplicationDbContext db, UserManager<ApplicationUser> userManager) =>
        {
            try
            {
                var team = await db.Teams
                    .Include(t => t.Members)
                    .FirstOrDefaultAsync(t => t.Id == id);
                    
                if (team is null)
                    return Results.NotFound(new { error = "Tým nenalezen." });

                string raw;
                using (var reader = new StreamReader(http.Body))
                    raw = await reader.ReadToEndAsync();

                using var doc = JsonDocument.Parse(raw);
                var root = doc.RootElement;

                var userId = root.TryGetProperty("userId", out var u) ? u.GetString() : null;
                var isLead = root.TryGetProperty("isLead", out var l) && l.GetBoolean();

                if (string.IsNullOrWhiteSpace(userId))
                    return Results.BadRequest(new { error = "UserId je povinné." });

                var user = await userManager.FindByIdAsync(userId);
                if (user is null)
                    return Results.NotFound(new { error = "Uživatel nenalezen." });

                // Zkontrolovat jestli už není aktivním členem
                var existing = team.Members.FirstOrDefault(m => m.UserId == userId && m.ToDate == null);
                if (existing != null)
                    return Results.BadRequest(new { error = "Uživatel je již členem týmu." });

                // Pokud má být vedoucí, nastavit LeadUserId
                if (isLead)
                {
                    team.LeadUserId = userId;
                }

                var membership = new TeamMembership
                {
                    Id = Guid.NewGuid(),
                    TeamId = id,
                    UserId = userId,
                    FromDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    ToDate = null,
                    Role = isLead ? "Lead" : "Member"
                };

                db.TeamMemberships.Add(membership);
                await db.SaveChangesAsync();

                return Results.Created($"/api/teams/{id}/members", new { membership.Id });
            }
            catch (Exception ex)
            {
                return Results.Problem(title: "Chyba při přidávání člena.", detail: ex.Message, statusCode: 500);
            }
        });

        // DELETE: odebrat člena z týmu (nastaví ToDate)
        g.MapDelete("/{id:guid}/members/{userId}", async (Guid id, string userId, ApplicationDbContext db) =>
        {
            var team = await db.Teams
                .Include(t => t.Members)
                .FirstOrDefaultAsync(t => t.Id == id);
                
            if (team is null)
                return Results.NotFound(new { error = "Tým nenalezen." });

            var membership = team.Members.FirstOrDefault(m => m.UserId == userId && m.ToDate == null);
            if (membership is null)
                return Results.NotFound(new { error = "Členství nenalezeno." });

            // Ukončit členství
            membership.ToDate = DateOnly.FromDateTime(DateTime.UtcNow);

            // Pokud byl vedoucí, odebrat
            if (team.LeadUserId == userId)
            {
                team.LeadUserId = null;
            }

            await db.SaveChangesAsync();

            return Results.NoContent();
        });

        // PUT: změnit vedoucího týmu
        g.MapPut("/{id:guid}/lead/{userId}", async (Guid id, string userId, ApplicationDbContext db) =>
        {
            var team = await db.Teams
                .Include(t => t.Members)
                .FirstOrDefaultAsync(t => t.Id == id);
                
            if (team is null)
                return Results.NotFound(new { error = "Tým nenalezen." });

            var membership = team.Members.FirstOrDefault(m => m.UserId == userId && m.ToDate == null);
            if (membership is null)
                return Results.NotFound(new { error = "Uživatel není aktivním členem týmu." });

            // Nastavit vedoucího
            team.LeadUserId = userId;

            // Aktualizovat role všech členů
            foreach (var member in team.Members.Where(m => m.ToDate == null))
            {
                member.Role = (member.UserId == userId) ? "Lead" : "Member";
            }

            await db.SaveChangesAsync();

            return Results.NoContent();
        });

        return app;
    }
}