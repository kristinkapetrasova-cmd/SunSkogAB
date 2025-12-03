using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SunSkog.Api.Data;
using SunSkog.Api.Models;
using SunSkog.Api.Storage.Entities;

namespace SunSkog.Api.Endpoints;

public static class AdminTeamEndpoints
{
    public static WebApplication MapAdminTeamEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/teams")
            .RequireAuthorization(policy => policy.RequireRole("Management", "Admin"));

        // GET /api/teams - seznam týmů
        group.MapGet("/", async (ApplicationDbContext db) =>
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            
            var teams = await db.Teams
                .Include(t => t.LeadUser)
                .ToListAsync();

            // Načíst počty členů přímo z TeamMemberships (ne přes navigační vlastnost)
            var memberCounts = await db.TeamMemberships
                .Where(m => m.ToDate == null || m.ToDate >= today)
                .GroupBy(m => m.TeamId)
                .Select(g => new { TeamId = g.Key, Count = g.Count() })
                .ToListAsync();

            var result = teams.Select(t => new
            {
                t.Id,
                t.Name,
                LeadUserId = t.LeadUserId,
                LeadUserName = t.LeadUser != null 
                    ? (t.LeadUser.FullName ?? t.LeadUser.UserName) 
                    : null,
                MemberCount = memberCounts.FirstOrDefault(mc => mc.TeamId == t.Id)?.Count ?? 0
            });

            return Results.Ok(result);
        });

        // GET /api/teams/{id} - detail týmu s členy
        group.MapGet("/{id:guid}", async (Guid id, ApplicationDbContext db, UserManager<ApplicationUser> userManager) =>
        {
            var team = await db.Teams
                .Include(t => t.LeadUser)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (team == null)
                return Results.NotFound("Tým nenalezen");

            var today = DateOnly.FromDateTime(DateTime.Today);
            
            // Načíst aktivní členství přímo z TeamMemberships (ne přes navigační vlastnost)
            var activeMembers = await db.TeamMemberships
                .Where(m => m.TeamId == id && (m.ToDate == null || m.ToDate >= today))
                .ToListAsync();

            // Načíst info o uživatelích
            var memberDetails = new List<object>();
            foreach (var m in activeMembers)
            {
                var user = await userManager.FindByIdAsync(m.UserId);
                if (user != null)
                {
                    memberDetails.Add(new
                    {
                        m.Id,
                        m.UserId,
                        UserName = user.FullName ?? user.UserName,
                        user.Email,
                        m.Role,
                        m.FromDate,
                        m.ToDate
                    });
                }
            }

            return Results.Ok(new
            {
                team.Id,
                team.Name,
                team.LeadUserId,
                LeadUserName = team.LeadUser != null 
                    ? (team.LeadUser.FullName ?? team.LeadUser.UserName) 
                    : null,
                Members = memberDetails
            });
        });

        // POST /api/teams - vytvořit tým
        group.MapPost("/", async (CreateTeamRequest req, ApplicationDbContext db) =>
        {
            var team = new Team
            {
                Name = req.Name
            };

            db.Teams.Add(team);
            await db.SaveChangesAsync();

            return Results.Created($"/api/teams/{team.Id}", new { team.Id, team.Name });
        });

        // PUT /api/teams/{id} - upravit tým
        group.MapPut("/{id:guid}", async (Guid id, UpdateTeamRequest req, ApplicationDbContext db) =>
        {
            var team = await db.Teams.FindAsync(id);
            if (team == null)
                return Results.NotFound("Tým nenalezen");

            team.Name = req.Name;
            await db.SaveChangesAsync();

            return Results.Ok(new { team.Id, team.Name });
        });

        // DELETE /api/teams/{id} - smazat tým
        group.MapDelete("/{id:guid}", async (Guid id, ApplicationDbContext db) =>
        {
            var team = await db.Teams.FindAsync(id);

            if (team == null)
                return Results.NotFound("Tým nenalezen");

            // Smazat všechna členství - načíst přímo z databáze
            var memberships = await db.TeamMemberships
                .Where(m => m.TeamId == id)
                .ToListAsync();
            
            db.TeamMemberships.RemoveRange(memberships);
            db.Teams.Remove(team);
            await db.SaveChangesAsync();

            return Results.NoContent();
        });

        // =====================================================
        // NASTAVENÍ VEDOUCÍHO
        // =====================================================
        
        // PUT /api/teams/{teamId}/lead/{userId} - nastavit vedoucího
        group.MapPut("/{teamId:guid}/lead/{userId}", async (
            Guid teamId, 
            string userId, 
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager) =>
        {
            var team = await db.Teams.FindAsync(teamId);
            if (team == null)
                return Results.NotFound("Tým nenalezen");

            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
                return Results.NotFound("Uživatel nenalezen");

            team.LeadUserId = userId;
            await db.SaveChangesAsync();

            return Results.Ok(new 
            { 
                team.Id, 
                team.Name, 
                LeadUserId = userId,
                LeadUserName = user.FullName ?? user.UserName
            });
        });

        // DELETE /api/teams/{teamId}/lead - odebrat vedoucího
        group.MapDelete("/{teamId:guid}/lead", async (Guid teamId, ApplicationDbContext db) =>
        {
            var team = await db.Teams.FindAsync(teamId);
            if (team == null)
                return Results.NotFound("Tým nenalezen");

            team.LeadUserId = null;
            await db.SaveChangesAsync();

            return Results.NoContent();
        });

        // =====================================================
        // SPRÁVA ČLENŮ
        // =====================================================

        // POST /api/teams/{teamId}/members - přidat člena
        group.MapPost("/{teamId:guid}/members", async (
            Guid teamId, 
            AddMemberRequest req, 
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager) =>
        {
            var team = await db.Teams.FindAsync(teamId);
            if (team == null)
                return Results.NotFound("Tým nenalezen");

            var user = await userManager.FindByIdAsync(req.UserId);
            if (user == null)
                return Results.NotFound("Uživatel nenalezen");

            var today = DateOnly.FromDateTime(DateTime.Today);

            // Kontrola: je uživatel už v TOMTO týmu?
            var existingInThisTeam = await db.TeamMemberships
                .AnyAsync(m => m.TeamId == teamId 
                    && m.UserId == req.UserId 
                    && (m.ToDate == null || m.ToDate >= today));

            if (existingInThisTeam)
            {
                return Results.Conflict(new 
                { 
                    error = "UserAlreadyInTeam",
                    message = "Uživatel je již členem tohoto týmu"
                });
            }

            // Kontrola: je uživatel v JINÉM týmu?
            var existingInOtherTeam = await db.TeamMemberships
                .Include(m => m.Team)
                .FirstOrDefaultAsync(m => m.TeamId != teamId 
                    && m.UserId == req.UserId 
                    && (m.ToDate == null || m.ToDate >= today));

            if (existingInOtherTeam != null)
            {
                // Vrátit info o existujícím členství - frontend se zeptá zda přesunout
                return Results.Conflict(new 
                { 
                    error = "UserInOtherTeam",
                    message = $"Uživatel je již členem týmu {existingInOtherTeam.Team?.Name}",
                    currentTeamId = existingInOtherTeam.TeamId,
                    currentTeamName = existingInOtherTeam.Team?.Name,
                    membershipId = existingInOtherTeam.Id
                });
            }

            // Přidat nové členství
            var membership = new TeamMembership
            {
                TeamId = teamId,
                UserId = req.UserId,
                FromDate = today,
                Role = "Member"
            };

            db.TeamMemberships.Add(membership);
            await db.SaveChangesAsync();

            return Results.Created($"/api/teams/{teamId}/members/{membership.Id}", new
            {
                membership.Id,
                membership.UserId,
                UserName = user.FullName ?? user.UserName,
                user.Email,
                membership.Role,
                membership.FromDate
            });
        });

        // PUT /api/teams/{teamId}/members/{userId}/transfer - přesunout člena z jiného týmu
        group.MapPut("/{teamId:guid}/members/{userId}/transfer", async (
            Guid teamId, 
            string userId, 
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager) =>
        {
            var team = await db.Teams.FindAsync(teamId);
            if (team == null)
                return Results.NotFound("Tým nenalezen");

            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
                return Results.NotFound("Uživatel nenalezen");

            var today = DateOnly.FromDateTime(DateTime.Today);

            // Ukončit všechna aktivní členství uživatele
            var activeMemeberships = await db.TeamMemberships
                .Where(m => m.UserId == userId && (m.ToDate == null || m.ToDate >= today))
                .ToListAsync();

            foreach (var m in activeMemeberships)
            {
                m.ToDate = today.AddDays(-1); // Ukončit včera
            }

            // Přidat nové členství v novém týmu
            var newMembership = new TeamMembership
            {
                TeamId = teamId,
                UserId = userId,
                FromDate = today,
                Role = "Member"
            };

            db.TeamMemberships.Add(newMembership);
            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                newMembership.Id,
                newMembership.UserId,
                UserName = user.FullName ?? user.UserName,
                user.Email,
                newMembership.Role,
                newMembership.FromDate
            });
        });

        // DELETE /api/teams/{teamId}/members/{userId} - odebrat člena
        group.MapDelete("/{teamId:guid}/members/{userId}", async (
            Guid teamId, 
            string userId, 
            ApplicationDbContext db) =>
        {
            var team = await db.Teams.FindAsync(teamId);
            if (team == null)
                return Results.NotFound(new { error = "Tým nenalezen" });

            var today = DateOnly.FromDateTime(DateTime.Today);
            var membership = await db.TeamMemberships
                .FirstOrDefaultAsync(m => m.UserId == userId && m.TeamId == teamId && (m.ToDate == null || m.ToDate >= today));

            if (membership == null)
                return Results.NotFound(new { error = "Členství nenalezeno" });

            // Ukončit členství (ne smazat - kvůli historii)
            // ToDate musí být včera, protože podmínka aktivních je ToDate >= today
            membership.ToDate = today.AddDays(-1);
            await db.SaveChangesAsync();

            return Results.NoContent();
        });

        return app;
    }
}

public record CreateTeamRequest(string Name);
public record UpdateTeamRequest(string Name);
public record AddMemberRequest(string UserId);