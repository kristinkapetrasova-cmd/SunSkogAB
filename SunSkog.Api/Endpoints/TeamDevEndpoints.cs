using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SunSkog.Api.Data;
using SunSkog.Api.Storage.Entities;

namespace SunSkog.Api.Endpoints;

public static class TeamDevEndpoints
{
    public static IEndpointRouteBuilder MapTeamDevEndpoints(this IEndpointRouteBuilder app)
    {
        var grp = app.MapGroup("/dev/teams").WithTags("Dev").AllowAnonymous();

        // POST /dev/teams/seed
        grp.MapPost("/seed", async (ApplicationDbContext db) =>
        {
            // 1) Týmy
            var existingTeams = await db.Teams.AsNoTracking().ToListAsync();

            var teamA = existingTeams.FirstOrDefault(t => t.Name == "Četa A");
            var teamB = existingTeams.FirstOrDefault(t => t.Name == "Četa B");

            if (teamA is null)
            {
                teamA = new Team { Name = "Četa A" };
                db.Teams.Add(teamA);
            }
            if (teamB is null)
            {
                teamB = new Team { Name = "Četa B" };
                db.Teams.Add(teamB);
            }
            await db.SaveChangesAsync();

            // 2) Aktivní členství pro všechny uživatele (střídavě A/B)
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var users = await db.Users.AsNoTracking().OrderBy(u => u.Email).ToListAsync();

            var memberships = await db.TeamMemberships.AsNoTracking().ToListAsync();
            var toAdd = new List<TeamMembership>();

            int i = 0;
            foreach (var u in users)
            {
                var alreadyActive = memberships.Any(m =>
                    m.UserId == u.Id &&
                    m.FromDate <= today &&
                    (m.ToDate == null || m.ToDate >= today));

                if (alreadyActive) continue;

                var team = (i++ % 2 == 0) ? teamA : teamB;

                toAdd.Add(new TeamMembership
                {
                    UserId = u.Id,
                    TeamId = team.Id,
                    FromDate = today
                });
            }

            if (toAdd.Count > 0)
            {
                db.TeamMemberships.AddRange(toAdd);
                await db.SaveChangesAsync();
            }

            return Results.Ok(new
            {
                createdTeams = new[] { teamA.Name, teamB.Name },
                newMemberships = toAdd.Count
            });
        })
        .WithOpenApi();

        return app;
    }
}