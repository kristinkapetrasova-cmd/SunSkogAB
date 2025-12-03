using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SunSkog.Api.Data;
using SunSkog.Api.Storage.Entities;

namespace SunSkog.Api.Endpoints;

public record CreateCategoryRequest(
    string Name,
    string? NameEn,
    bool HasSizes,
    bool HasItemTypes,
    int SortOrder
);

public record UpdateCategoryRequest(
    string Name,
    string? NameEn,
    bool HasSizes,
    bool HasItemTypes,
    int SortOrder,
    bool IsActive
);

public static class CategoryEndpoints
{
    public static IEndpointRouteBuilder MapCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        var grp = app.MapGroup("/api/categories")
            .WithTags("Categories")
            .RequireAuthorization(policyNames: new[] { "CanUseInventory" });

        // GET all categories
        grp.MapGet("/", async (ApplicationDbContext db) =>
        {
            var categories = await db.Categories
                .AsNoTracking()
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.Name)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.NameEn,
                    c.HasSizes,
                    c.HasItemTypes,
                    c.SortOrder,
                    c.IsActive,
                    ItemCount = c.Items.Count(i => i.IsActive)
                })
                .ToListAsync();

            return Results.Ok(categories);
        });

        // GET single category
        grp.MapGet("/{id:guid}", async (Guid id, ApplicationDbContext db) =>
        {
            var category = await db.Categories
                .AsNoTracking()
                .Where(c => c.Id == id)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.NameEn,
                    c.HasSizes,
                    c.HasItemTypes,
                    c.SortOrder,
                    c.IsActive,
                    ItemCount = c.Items.Count(i => i.IsActive)
                })
                .FirstOrDefaultAsync();

            return category is null ? Results.NotFound() : Results.Ok(category);
        });

        // POST create category (Admin only)
        grp.MapPost("/", async ([FromBody] CreateCategoryRequest req, ApplicationDbContext db) =>
        {
            var category = new Category
            {
                Id = Guid.NewGuid(),
                Name = req.Name,
                NameEn = req.NameEn,
                HasSizes = req.HasSizes,
                HasItemTypes = req.HasItemTypes,
                SortOrder = req.SortOrder,
                IsActive = true
            };

            db.Categories.Add(category);
            await db.SaveChangesAsync();

            return Results.Created($"/api/categories/{category.Id}", new
            {
                category.Id,
                category.Name,
                category.NameEn,
                category.HasSizes,
                category.HasItemTypes,
                category.SortOrder,
                category.IsActive,
                ItemCount = 0
            });
        }).RequireAuthorization(policy => policy.RequireRole("Management", "Admin"));

        // PUT update category (Admin only)
        grp.MapPut("/{id:guid}", async (Guid id, [FromBody] UpdateCategoryRequest req, ApplicationDbContext db) =>
        {
            var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == id);
            if (category is null) return Results.NotFound();

            category.Name = req.Name;
            category.NameEn = req.NameEn;
            category.HasSizes = req.HasSizes;
            category.HasItemTypes = req.HasItemTypes;
            category.SortOrder = req.SortOrder;
            category.IsActive = req.IsActive;

            await db.SaveChangesAsync();

            var itemCount = await db.InventoryItems.CountAsync(i => i.CategoryId == id && i.IsActive);

            return Results.Ok(new
            {
                category.Id,
                category.Name,
                category.NameEn,
                category.HasSizes,
                category.HasItemTypes,
                category.SortOrder,
                category.IsActive,
                ItemCount = itemCount
            });
        }).RequireAuthorization(policy => policy.RequireRole("Management", "Admin"));

        // DELETE category (Admin only) - pouze pokud nemá položky
        grp.MapDelete("/{id:guid}", async (Guid id, ApplicationDbContext db) =>
        {
            var category = await db.Categories
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category is null) return Results.NotFound();

            if (category.Items.Any(i => i.IsActive))
            {
                return Results.BadRequest(new { message = "Nelze smazat kategorii s aktivními položkami." });
            }

            db.Categories.Remove(category);
            await db.SaveChangesAsync();

            return Results.NoContent();
        }).RequireAuthorization(policy => policy.RequireRole("Management", "Admin"));

        return app;
    }
}