using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SunSkog.Api.Data;
using SunSkog.Api.Models;
using SunSkog.Api.Storage.Entities;

namespace SunSkog.Api.Seeds;

public static class AppDataSeeder
{
    public static async Task SeedAsync(IServiceProvider services, ILogger logger)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;

        var env = sp.GetRequiredService<IHostEnvironment>();
        if (!env.IsDevelopment())
        {
            logger.LogInformation("AppDataSeeder: skipping (not Development).");
            return;
        }

        var db = sp.GetRequiredService<ApplicationDbContext>();
        var users = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = sp.GetRequiredService<RoleManager<IdentityRole>>();

        // 1) Role
        var roleNames = new[] { "Admin", "Accountant", "Management", "TeamLead", "Warehouse", "User" };
        foreach (var rn in roleNames)
        {
            if (!await roles.RoleExistsAsync(rn))
            {
                var res = await roles.CreateAsync(new IdentityRole(rn));
                if (!res.Succeeded)
                {
                    var err = string.Join("; ", res.Errors.Select(e => e.Description));
                    logger.LogWarning("AppDataSeeder: role '{Role}' create failed: {Err}", rn, err);
                }
            }
        }

        // 2) Users (lead + worker)
        var leadEmail   = "lead@sunskog.local";
        var workerEmail = "worker@sunskog.local";

        var lead = await users.FindByEmailAsync(leadEmail);
        if (lead is null)
        {
            lead = new ApplicationUser
            {
                UserName = leadEmail,
                Email = leadEmail,
                EmailConfirmed = true,
                FullName = "Team Lead"
            };
            var create = await users.CreateAsync(lead, "Lead123!");
            if (!create.Succeeded)
            {
                var err = string.Join("; ", create.Errors.Select(e => e.Description));
                logger.LogError("AppDataSeeder: create lead failed: {Err}", err);
                return;
            }
        }
        if (!await users.IsInRoleAsync(lead, "TeamLead"))
            await users.AddToRoleAsync(lead, "TeamLead");

        var worker = await users.FindByEmailAsync(workerEmail);
        if (worker is null)
        {
            worker = new ApplicationUser
            {
                UserName = workerEmail,
                Email = workerEmail,
                EmailConfirmed = true,
                FullName = "Demo Worker"
            };
            var create = await users.CreateAsync(worker, "Worker123!");
            if (!create.Succeeded)
            {
                var err = string.Join("; ", create.Errors.Select(e => e.Description));
                logger.LogError("AppDataSeeder: create worker failed: {Err}", err);
                return;
            }
        }
        if (!await users.IsInRoleAsync(worker, "User"))
            await users.AddToRoleAsync(worker, "User");

        // 3) Team "Parta A"
        var team = await db.Teams.FirstOrDefaultAsync(t => t.Name == "Parta A");
        if (team is null)
        {
            team = new Team
            {
                Name = "Parta A",
                LeadUserId = lead.Id
            };
            db.Teams.Add(team);
            await db.SaveChangesAsync();
        }
        else
        {
            // pro jistotu aktualizuj vedoucího
            if (team.LeadUserId != lead.Id)
            {
                team.LeadUserId = lead.Id;
                await db.SaveChangesAsync();
            }
        }

        // 4) Memberships (Lead + Member)
        async Task EnsureMemberAsync(string userId, string role, DateOnly from)
        {
            var exists = await db.TeamMemberships.AnyAsync(x => x.TeamId == team.Id && x.UserId == userId);
            if (!exists)
            {
                db.TeamMemberships.Add(new TeamMembership
                {
                    TeamId   = team.Id,
                    UserId   = userId,
                    Role     = role,               // "Lead" | "Member"
                    FromDate = from,
                    ToDate   = null
                });
                await db.SaveChangesAsync();
            }
        }

        await EnsureMemberAsync(lead.Id,   "Lead",   new DateOnly(2025, 10, 1));
        await EnsureMemberAsync(worker.Id, "Member", new DateOnly(2025, 10, 1));

        // 5) Timesheet pro worker (1.–15.10.2025) + 2 entries
        var periodStart = new DateOnly(2025, 10, 1);
        var periodEnd   = new DateOnly(2025, 10, 15);

        var hasTs = await db.Timesheets.AnyAsync(t =>
            t.EmployeeId == worker.Id &&
            t.PeriodStart == periodStart &&
            t.PeriodEnd == periodEnd);

        if (!hasTs)
        {
            var ts = new Timesheet
            {
                EmployeeId = worker.Id,
                PeriodStart = periodStart,
                PeriodEnd   = periodEnd,
                Status = TimesheetStatus.Draft,
                Notes  = "Seed demo"
            };

            var e1 = new TimesheetEntry
            {
                TimesheetId = ts.Id,
                WorkDate = new DateOnly(2025, 10, 3),
                Project = "Projekt A",
                Task = "Tezba",
                Hours = 6.0m,
                Km = 10m,
                Pieces = 0,
                HourRate = 200m,
                KmRate = 8m,
                PieceRate = 0m,
                Comment = "Seed e1"
            };
            e1.EntryPay = e1.Hours * e1.HourRate + e1.Km * e1.KmRate + e1.Pieces * e1.PieceRate;

            var e2 = new TimesheetEntry
            {
                TimesheetId = ts.Id,
                WorkDate = new DateOnly(2025, 10, 8),
                Project = "Projekt B",
                Task = "Vyklizeni",
                Hours = 7.5m,
                Km = 12m,
                Pieces = 0,
                HourRate = 200m,
                KmRate = 8m,
                PieceRate = 0m,
                Comment = "Seed e2"
            };
            e2.EntryPay = e2.Hours * e2.HourRate + e2.Km * e2.KmRate + e2.Pieces * e2.PieceRate;

            ts.Entries.Add(e1);
            ts.Entries.Add(e2);

            // přepočty
            ts.TotalHours  = ts.Entries.Sum(x => x.Hours);
            ts.TotalKm     = ts.Entries.Sum(x => x.Km);
            ts.TotalPieces = ts.Entries.Sum(x => x.Pieces);
            ts.TotalPay    = ts.Entries.Sum(x => x.EntryPay);

            db.Timesheets.Add(ts);
            await db.SaveChangesAsync();
        }

        logger.LogInformation("AppDataSeeder: completed.");
    }
}