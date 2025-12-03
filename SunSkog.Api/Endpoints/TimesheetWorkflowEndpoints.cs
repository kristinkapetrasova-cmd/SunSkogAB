using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SunSkog.Api.Data;
using SunSkog.Api.Models;
using SunSkog.Api.Storage.Entities;

namespace SunSkog.Api.Endpoints
{
    public static class TimesheetWorkflowEndpoints
    {
        public static IEndpointRouteBuilder MapTimesheetWorkflowEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/timesheets")
                           .WithTags("Timesheets")
                           .RequireAuthorization();

            // SUBMIT - pouze vlastník může odeslat svůj výkaz
            group.MapPost("/{id:guid}/submit", async (
                Guid id,
                ApplicationDbContext db,
                UserManager<ApplicationUser> userManager,
                HttpContext http
            ) =>
            {
                var userId = GetUserId(http.User);
                if (userId is null) return Results.Unauthorized();

                var ts = await db.Timesheets.FirstOrDefaultAsync(x => x.Id == id);
                if (ts == null) return Results.NotFound(new { error = "Timesheet not found" });

                // Kontrola: pouze vlastník může odeslat
                if (ts.EmployeeId != userId)
                    return Results.Forbid();

                if (ts.Status != TimesheetStatus.Draft && ts.Status != TimesheetStatus.Returned)
                    return Results.Problem(statusCode: 409, title: "Invalid state", detail: "Only Draft or Returned can be submitted.");

                ts.Status = TimesheetStatus.Submitted;
                ts.SubmittedAt = DateTime.UtcNow;

                db.ApprovalLogs.Add(new ApprovalLog
                {
                    Id = Guid.NewGuid(),
                    TimesheetId = ts.Id,
                    Action = ApprovalAction.Submit,
                    ByUserId = userId,
                    At = DateTime.UtcNow,
                    Note = "Submitted"
                });

                await db.SaveChangesAsync();
                return Results.Ok(new { id = ts.Id, status = ts.Status.ToString(), ts.SubmittedAt });
            });

            // APPROVE - pouze TeamLead/Management/Admin, ne vlastní výkaz
            group.MapPost("/{id:guid}/approve", async (
                Guid id,
                ApplicationDbContext db,
                UserManager<ApplicationUser> userManager,
                HttpContext http
            ) =>
            {
                var userId = GetUserId(http.User);
                if (userId is null) return Results.Unauthorized();

                // Kontrola role
                var canApprove = http.User.IsInRole("TeamLead") ||
                                 http.User.IsInRole("Management") ||
                                 http.User.IsInRole("Admin") ||
                                 http.User.IsInRole("Manager") ||
                                 http.User.IsInRole("SuperAdmin");

                if (!canApprove)
                    return Results.Forbid();

                var ts = await db.Timesheets.Include(t => t.Entries).FirstOrDefaultAsync(x => x.Id == id);
                if (ts == null) return Results.NotFound(new { error = "Timesheet not found" });

                // Kontrola: nelze schválit vlastní výkaz
                if (ts.EmployeeId == userId)
                    return Results.Problem(statusCode: 403, title: "Forbidden", detail: "Cannot approve your own timesheet.");

                if (ts.Status != TimesheetStatus.Submitted)
                    return Results.Problem(statusCode: 409, title: "Invalid state", detail: "Only Submitted can be approved.");

                ts.Status = TimesheetStatus.Approved;
                ts.ApprovedAt = DateTime.UtcNow;

                db.ApprovalLogs.Add(new ApprovalLog
                {
                    Id = Guid.NewGuid(),
                    TimesheetId = ts.Id,
                    Action = ApprovalAction.Approve,
                    ByUserId = userId,
                    At = DateTime.UtcNow,
                    Note = "Approved"
                });

                await db.SaveChangesAsync();
                return Results.Ok(new { id = ts.Id, status = ts.Status.ToString(), ts.ApprovedAt });
            });

            // RETURN - pouze TeamLead/Management/Admin, ne vlastní výkaz
            group.MapPost("/{id:guid}/return", async (
                Guid id,
                [FromBody] ReturnRequest? body,
                ApplicationDbContext db,
                UserManager<ApplicationUser> userManager,
                HttpContext http
            ) =>
            {
                var userId = GetUserId(http.User);
                if (userId is null) return Results.Unauthorized();

                // Kontrola role
                var canReturn = http.User.IsInRole("TeamLead") ||
                                http.User.IsInRole("Management") ||
                                http.User.IsInRole("Admin") ||
                                http.User.IsInRole("Manager") ||
                                http.User.IsInRole("SuperAdmin");

                if (!canReturn)
                    return Results.Forbid();

                var ts = await db.Timesheets.FirstOrDefaultAsync(x => x.Id == id);
                if (ts == null) return Results.NotFound(new { error = "Timesheet not found" });

                // Kontrola: nelze vrátit vlastní výkaz
                if (ts.EmployeeId == userId)
                    return Results.Problem(statusCode: 403, title: "Forbidden", detail: "Cannot return your own timesheet.");

                if (ts.Status != TimesheetStatus.Submitted)
                    return Results.Problem(statusCode: 409, title: "Invalid state", detail: "Only Submitted can be returned.");

                ts.Status = TimesheetStatus.Returned;
                ts.ApprovedAt = null;

                db.ApprovalLogs.Add(new ApprovalLog
                {
                    Id = Guid.NewGuid(),
                    TimesheetId = ts.Id,
                    Action = ApprovalAction.Return,
                    ByUserId = userId,
                    At = DateTime.UtcNow,
                    Note = string.IsNullOrWhiteSpace(body?.Note) ? "Returned" : body!.Note
                });

                await db.SaveChangesAsync();
                return Results.Ok(new { id = ts.Id, status = ts.Status.ToString() });
            });

            // LOGS
            group.MapGet("/{id:guid}/logs", async (Guid id, ApplicationDbContext db) =>
            {
                var exists = await db.Timesheets.AnyAsync(t => t.Id == id);
                if (!exists) return Results.NotFound(new { error = "Timesheet not found" });

                var logs = await db.ApprovalLogs
                    .Where(a => a.TimesheetId == id)
                    .OrderBy(a => a.At)
                    .Select(a => new
                    {
                        a.Id,
                        a.TimesheetId,
                        action = a.Action.ToString(),
                        a.ByUserId,
                        a.At,
                        a.Note
                    })
                    .ToListAsync();

                return Results.Ok(logs);
            });

            return app;
        }

        private static string? GetUserId(ClaimsPrincipal user)
            => user.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);

        public sealed record ReturnRequest(string? Note);
    }
}
