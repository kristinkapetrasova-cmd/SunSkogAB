using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SunSkog.Api.Data;
using SunSkog.Api.Storage.Entities;
using SunSkog.Api.Models;
using System.Security.Claims;

namespace SunSkog.Api.Endpoints;

public static class AssignmentEndpoints
{
    public static IEndpointRouteBuilder MapAssignmentEndpoints(this IEndpointRouteBuilder app)
    {
        var grp = app.MapGroup("/api/assignments")
            .WithTags("Assignments")
            .RequireAuthorization();

        // GET: všechny aktivní přiřazení (pro admin/warehouse/teamlead)
        grp.MapGet("/", async (ApplicationDbContext db, UserManager<ApplicationUser> userManager) =>
        {
            var assignments = await db.Assignments
                .AsNoTracking()
                .Include(a => a.Item)
                    .ThenInclude(i => i!.Category)
                .Include(a => a.Team)
                .Where(a => a.ReturnedAt == null)
                .OrderByDescending(a => a.AssignedAt)
                .ToListAsync();

            var userIds = assignments
                .Where(a => !string.IsNullOrEmpty(a.UserId))
                .Select(a => a.UserId!)
                .Distinct()
                .ToList();
            
            var users = await userManager.Users
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new { u.Id, Name = u.FullName ?? u.UserName, u.Email })
                .ToListAsync();

            var result = assignments.Select(a =>
            {
                var assignedUser = users.FirstOrDefault(u => u.Id == a.UserId);
                return new
                {
                    a.Id,
                    a.ItemId,
                    ItemName = a.Item?.Name,
                    ItemSKU = a.Item?.SKU,
                    CategoryName = a.Item?.Category?.Name,
                    Size = a.Item?.Size,
                    ItemType = a.Item?.ItemType,
                    EmployeeId = a.UserId,
                    EmployeeName = assignedUser?.Name ?? (a.UserId != null ? "Unknown" : null),
                    EmployeeEmail = assignedUser?.Email,
                    a.TeamId,
                    TeamName = a.Team?.Name,
                    a.Quantity,
                    a.AssignedAt,
                    a.Note
                };
            });

            return Results.Ok(result);
        });

        // GET: přiřazení pro aktuálního uživatele (pro worker)
        grp.MapGet("/my", async (ApplicationDbContext db, ClaimsPrincipal user) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            var assignments = await db.Assignments
                .AsNoTracking()
                .Include(a => a.Item)
                    .ThenInclude(i => i!.Category)
                .Where(a => a.UserId == userId && a.ReturnedAt == null)
                .OrderByDescending(a => a.AssignedAt)
                .Select(a => new
                {
                    a.Id,
                    a.ItemId,
                    ItemName = a.Item != null ? a.Item.Name : "Unknown",
                    ItemSKU = a.Item != null ? a.Item.SKU : null,
                    CategoryName = a.Item != null && a.Item.Category != null ? a.Item.Category.Name : null,
                    Size = a.Item != null ? a.Item.Size : null,
                    ItemType = a.Item != null ? a.Item.ItemType : null,
                    a.Quantity,
                    a.AssignedAt,
                    a.Note
                })
                .ToListAsync();

            return Results.Ok(assignments);
        });

        // GET: přiřazení pro tým (pro TeamLead)
        grp.MapGet("/team", async (ApplicationDbContext db, UserManager<ApplicationUser> userManager, ClaimsPrincipal user) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            // Najít týmy kde je uživatel vedoucím
            var teamIds = await db.Teams
                .AsNoTracking()
                .Where(t => t.LeadUserId == userId)
                .Select(t => t.Id)
                .ToListAsync();

            if (!teamIds.Any())
                return Results.Ok(new List<object>());

            // Najít všechny členy týmů
            var teamMemberIds = await db.TeamMemberships
                .AsNoTracking()
                .Where(tm => teamIds.Contains(tm.TeamId) && tm.ToDate == null)
                .Select(tm => tm.UserId)
                .Distinct()
                .ToListAsync();

            // Přidat i sebe (TeamLead)
            if (!teamMemberIds.Contains(userId))
                teamMemberIds.Add(userId);

            // Najít přiřazení pro členy týmu NEBO přímo pro týmy
            var assignments = await db.Assignments
                .AsNoTracking()
                .Include(a => a.Item)
                    .ThenInclude(i => i!.Category)
                .Include(a => a.Team)
                .Where(a => a.ReturnedAt == null && 
                    (teamMemberIds.Contains(a.UserId!) || teamIds.Contains(a.TeamId ?? Guid.Empty)))
                .OrderByDescending(a => a.AssignedAt)
                .ToListAsync();

            // Načíst jména uživatelů
            var userIds = assignments
                .Where(a => !string.IsNullOrEmpty(a.UserId))
                .Select(a => a.UserId!)
                .Distinct()
                .ToList();

            var users = await userManager.Users
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new { u.Id, Name = u.FullName ?? u.UserName, u.Email })
                .ToListAsync();

            var result = assignments.Select(a =>
            {
                var assignedUser = users.FirstOrDefault(u => u.Id == a.UserId);
                return new
                {
                    a.Id,
                    a.ItemId,
                    ItemName = a.Item?.Name,
                    ItemSKU = a.Item?.SKU,
                    CategoryName = a.Item?.Category?.Name,
                    Size = a.Item?.Size,
                    ItemType = a.Item?.ItemType,
                    EmployeeId = a.UserId,
                    EmployeeName = assignedUser?.Name ?? (a.UserId != null ? "Unknown" : null),
                    EmployeeEmail = assignedUser?.Email,
                    a.TeamId,
                    TeamName = a.Team?.Name,
                    a.Quantity,
                    a.AssignedAt,
                    a.Note
                };
            });

            return Results.Ok(result);
        });

        // POST: přiřadit položku zaměstnanci nebo týmu
        grp.MapPost("/", async ([FromBody] CreateAssignmentRequest req, ApplicationDbContext db, UserManager<ApplicationUser> userManager) =>
        {
            // Validace položky
            var item = await db.InventoryItems
                .Include(i => i.Category)
                .FirstOrDefaultAsync(i => i.Id == req.ItemId);
            if (item == null)
                return Results.BadRequest(new { error = "Položka nenalezena." });

            // Validace - musí být buď zaměstnanec nebo tým
            if (string.IsNullOrEmpty(req.EmployeeId) && !req.TeamId.HasValue)
                return Results.BadRequest(new { error = "Musí být vybrán zaměstnanec nebo tým." });

            string? employeeName = null;
            string? teamName = null;

            if (!string.IsNullOrEmpty(req.EmployeeId))
            {
                var user = await userManager.FindByIdAsync(req.EmployeeId);
                if (user == null)
                    return Results.BadRequest(new { error = "Uživatel nenalezen." });
                employeeName = user.FullName ?? user.UserName;
            }

            if (req.TeamId.HasValue)
            {
                var team = await db.Teams.FirstOrDefaultAsync(t => t.Id == req.TeamId.Value);
                if (team == null)
                    return Results.BadRequest(new { error = "Tým nenalezen." });
                teamName = team.Name;
            }

            var assignment = new Assignment
            {
                Id = Guid.NewGuid(),
                ItemId = req.ItemId,
                UserId = string.IsNullOrEmpty(req.EmployeeId) ? null : req.EmployeeId,
                TeamId = req.TeamId,
                Quantity = req.Quantity > 0 ? req.Quantity : 1,
                AssignedAt = DateTime.UtcNow,
                Note = req.Note
            };

            db.Assignments.Add(assignment);
            await db.SaveChangesAsync();

            return Results.Created($"/api/assignments/{assignment.Id}", new
            {
                assignment.Id,
                assignment.ItemId,
                ItemName = item.Name,
                CategoryName = item.Category?.Name,
                Size = item.Size,
                EmployeeId = assignment.UserId,
                EmployeeName = employeeName,
                assignment.TeamId,
                TeamName = teamName,
                assignment.Quantity,
                assignment.AssignedAt,
                assignment.Note
            });
        });

        // DELETE: vrátit položku (soft delete - nastaví ReturnedAt)
        grp.MapDelete("/{id:guid}", async (Guid id, ApplicationDbContext db) =>
        {
            var assignment = await db.Assignments.FirstOrDefaultAsync(a => a.Id == id);
            if (assignment == null)
                return Results.NotFound(new { error = "Přiřazení nenalezeno." });

            assignment.ReturnedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return Results.NoContent();
        });

        // GET: historie přiřazení pro položku
        grp.MapGet("/item/{itemId:guid}", async (Guid itemId, ApplicationDbContext db, UserManager<ApplicationUser> userManager) =>
        {
            var assignments = await db.Assignments
                .AsNoTracking()
                .Include(a => a.Team)
                .Where(a => a.ItemId == itemId)
                .OrderByDescending(a => a.AssignedAt)
                .ToListAsync();

            var userIds = assignments
                .Where(a => !string.IsNullOrEmpty(a.UserId))
                .Select(a => a.UserId!)
                .Distinct()
                .ToList();

            var users = await userManager.Users
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new { u.Id, Name = u.FullName ?? u.UserName })
                .ToListAsync();

            var result = assignments.Select(a =>
            {
                var assignedUser = users.FirstOrDefault(u => u.Id == a.UserId);
                return new
                {
                    a.Id,
                    EmployeeName = assignedUser?.Name,
                    a.TeamId,
                    TeamName = a.Team?.Name,
                    a.Quantity,
                    a.AssignedAt,
                    a.ReturnedAt,
                    a.Note,
                    IsActive = a.ReturnedAt == null
                };
            });

            return Results.Ok(result);
        });

        return app;
    }
}

public record CreateAssignmentRequest(
    Guid ItemId, 
    string? EmployeeId = null, 
    Guid? TeamId = null,
    int Quantity = 1, 
    string? Note = null
);