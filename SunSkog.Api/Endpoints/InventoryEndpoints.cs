using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SunSkog.Api.Data;
using SunSkog.Api.Storage.Entities;

namespace SunSkog.Api.Endpoints;

// DTO pro příjem požadavků z frontendu
public record StockMovementRequest(Guid ItemId, decimal Quantity, string? Note, DateTime? MovementDate);

public record CreateItemRequest(
    string Name,
    string? SKU,
    string? SerialNumber,
    int MinStock,
    Guid? CategoryId,
    string? Size,
    string? ItemType
);

public record UpdateItemRequest(
    string Name,
    string? SKU,
    string? SerialNumber,
    int MinStock,
    bool IsActive,
    Guid? CategoryId,
    string? Size,
    string? ItemType
);

public static class InventoryEndpoints
{
    public static IEndpointRouteBuilder MapInventoryEndpoints(this IEndpointRouteBuilder app)
    {
        var grp = app.MapGroup("/api/inventory")
            .WithTags("Inventory")
            .RequireAuthorization(policyNames: new[] { "CanUseInventory" });

        // --- Items ---
        grp.MapGet("/items", async ([FromQuery] string? q, [FromQuery] Guid? categoryId, ApplicationDbContext db) =>
        {
            var query = db.InventoryItems
                .AsNoTracking()
                .Include(i => i.Category)
                .OrderBy(i => i.Category != null ? i.Category.SortOrder : 999)
                .ThenBy(i => i.Name)
                .ThenBy(i => i.Size)
                .AsQueryable();

            // Filtr podle kategorie
            if (categoryId.HasValue)
            {
                query = query.Where(i => i.CategoryId == categoryId.Value);
            }

            // Filtr podle textu
            if (!string.IsNullOrWhiteSpace(q))
            {
                query = query.Where(i =>
                    i.Name.Contains(q) ||
                    (i.SKU != null && i.SKU.Contains(q)) ||
                    (i.Size != null && i.Size.Contains(q)) ||
                    (i.ItemType != null && i.ItemType.Contains(q)));
            }

            var items = await query.Select(i => new
            {
                i.Id,
                i.Name,
                i.SKU,
                i.SerialNumber,
                i.MinStock,
                i.IsActive,
                i.CategoryId,
                CategoryName = i.Category != null ? i.Category.Name : null,
                i.Size,
                i.ItemType,
                i.CreatedAt,
                i.UpdatedAt
            }).ToListAsync();

            return Results.Ok(items);
        });

        grp.MapGet("/items/{id:guid}", async (Guid id, ApplicationDbContext db) =>
        {
            var item = await db.InventoryItems
                .AsNoTracking()
                .Include(i => i.Category)
                .Where(i => i.Id == id)
                .Select(i => new
                {
                    i.Id,
                    i.Name,
                    i.SKU,
                    i.SerialNumber,
                    i.MinStock,
                    i.IsActive,
                    i.CategoryId,
                    CategoryName = i.Category != null ? i.Category.Name : null,
                    CategoryHasSizes = i.Category != null && i.Category.HasSizes,
                    CategoryHasItemTypes = i.Category != null && i.Category.HasItemTypes,
                    i.Size,
                    i.ItemType,
                    i.CreatedAt,
                    i.UpdatedAt
                })
                .FirstOrDefaultAsync();

            return item is null ? Results.NotFound() : Results.Ok(item);
        });

        grp.MapPost("/items", async ([FromBody] CreateItemRequest req, ApplicationDbContext db) =>
        {
            var item = new InventoryItem
            {
                Id = Guid.NewGuid(),
                Name = req.Name,
                SKU = req.SKU,
                SerialNumber = req.SerialNumber,
                MinStock = req.MinStock,
                CategoryId = req.CategoryId,
                Size = req.Size,
                ItemType = req.ItemType,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            db.InventoryItems.Add(item);
            await db.SaveChangesAsync();

            // Načíst kategorii pro response
            var category = req.CategoryId.HasValue 
                ? await db.Categories.FindAsync(req.CategoryId.Value) 
                : null;

            return Results.Created($"/api/inventory/items/{item.Id}", new
            {
                item.Id,
                item.Name,
                item.SKU,
                item.SerialNumber,
                item.MinStock,
                item.IsActive,
                item.CategoryId,
                CategoryName = category?.Name,
                item.Size,
                item.ItemType,
                item.CreatedAt
            });
        });

        grp.MapPut("/items/{id:guid}", async (Guid id, [FromBody] UpdateItemRequest req, ApplicationDbContext db) =>
        {
            var item = await db.InventoryItems.FirstOrDefaultAsync(i => i.Id == id);
            if (item is null) return Results.NotFound();

            item.Name = req.Name;
            item.SKU = req.SKU;
            item.SerialNumber = req.SerialNumber;
            item.MinStock = req.MinStock;
            item.IsActive = req.IsActive;
            item.CategoryId = req.CategoryId;
            item.Size = req.Size;
            item.ItemType = req.ItemType;
            item.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            var category = req.CategoryId.HasValue
                ? await db.Categories.FindAsync(req.CategoryId.Value)
                : null;

            return Results.Ok(new
            {
                item.Id,
                item.Name,
                item.SKU,
                item.SerialNumber,
                item.MinStock,
                item.IsActive,
                item.CategoryId,
                CategoryName = category?.Name,
                item.Size,
                item.ItemType,
                item.CreatedAt,
                item.UpdatedAt
            });
        });

        grp.MapDelete("/items/{id:guid}", async (Guid id, ApplicationDbContext db) =>
        {
            var item = await db.InventoryItems.FirstOrDefaultAsync(i => i.Id == id);
            if (item is null) return Results.NotFound();
            db.InventoryItems.Remove(item);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        // --- Movements (příjem/výdej) ---
        grp.MapGet("/movements", async ([FromQuery] Guid? itemId, ApplicationDbContext db) =>
        {
            var q = db.StockMovements
                .AsNoTracking()
                .Include(m => m.Item)
                .OrderByDescending(m => m.At)
                .AsQueryable();

            if (itemId is Guid iid)
                q = q.Where(m => m.ItemId == iid);

            var list = await q.Select(m => new
            {
                m.Id,
                m.ItemId,
                ItemName = m.Item != null ? m.Item.Name : null,
                m.Quantity,
                m.At,
                m.Note,
                m.Type
            }).ToListAsync();

            return Results.Ok(list);
        });

        // Příjem/výdej s datem
        grp.MapPost("/movements", async ([FromBody] StockMovementRequest req, ApplicationDbContext db) =>
        {
            if (req.Quantity == 0) 
                return Results.BadRequest(new { message = "Quantity cannot be 0." });

            var item = await db.InventoryItems.FirstOrDefaultAsync(i => i.Id == req.ItemId);
            if (item is null) 
                return Results.BadRequest(new { message = "Item not found." });

            // Pro výdej (záporné číslo) zkontrolujeme, zda je dostatek na skladě
            if (req.Quantity < 0)
            {
                var currentStock = await db.StockMovements
                    .Where(m => m.ItemId == req.ItemId)
                    .SumAsync(m => (decimal?)m.Quantity) ?? 0m;

                if (currentStock + req.Quantity < 0)
                {
                    return Results.BadRequest(new { message = $"Nedostatek na skladě. Dostupné: {currentStock}" });
                }
            }

            var movement = new StockMovement
            {
                Id = Guid.NewGuid(),
                ItemId = req.ItemId,
                Quantity = req.Quantity,
                Note = req.Note,
                At = req.MovementDate ?? DateTime.UtcNow,  // Použít zadané datum nebo aktuální
                Type = req.Quantity > 0 ? MovementType.In : MovementType.Out
            };

            db.StockMovements.Add(movement);
            await db.SaveChangesAsync();
            
            return Results.Created($"/api/inventory/movements/{movement.Id}", new 
            { 
                movement.Id, 
                movement.ItemId, 
                ItemName = item.Name,
                movement.Quantity, 
                movement.At,
                movement.Note,
                movement.Type
            });
        });

        // --- Low stock (upozornění) ---
        grp.MapGet("/low-stock", async (ApplicationDbContext db) =>
        {
            var stocks = await db.InventoryItems
                .Include(i => i.Category)
                .Select(i => new
                {
                    item = i,
                    qty = db.StockMovements.Where(m => m.ItemId == i.Id)
                                           .Sum(m => (decimal?)m.Quantity) ?? 0m
                })
                .ToListAsync();

            var low = stocks.Where(s => s.item.IsActive && s.item.MinStock > 0 && s.qty < s.item.MinStock)
                            .Select(s => new
                            {
                                s.item.Id,
                                s.item.Name,
                                s.item.SKU,
                                s.item.Size,
                                s.item.ItemType,
                                CategoryName = s.item.Category?.Name,
                                current = s.qty,
                                min = s.item.MinStock
                            });
            return Results.Ok(low);
        });

        // --- Aktuální stav skladu pro všechny položky ---
        grp.MapGet("/stock", async ([FromQuery] Guid? categoryId, ApplicationDbContext db) =>
        {
            var query = db.InventoryItems.Include(i => i.Category).AsQueryable();

            if (categoryId.HasValue)
            {
                query = query.Where(i => i.CategoryId == categoryId.Value);
            }

            var stocks = await query
                .Select(i => new
                {
                    itemId = i.Id,
                    name = i.Name,
                    sku = i.SKU,
                    size = i.Size,
                    itemType = i.ItemType,
                    categoryId = i.CategoryId,
                    categoryName = i.Category != null ? i.Category.Name : null,
                    quantity = db.StockMovements.Where(m => m.ItemId == i.Id)
                                               .Sum(m => (decimal?)m.Quantity) ?? 0m,
                    minStock = i.MinStock,
                    isActive = i.IsActive
                })
                .OrderBy(s => s.categoryName)
                .ThenBy(s => s.name)
                .ThenBy(s => s.size)
                .ToListAsync();

            return Results.Ok(stocks);
        });

        return app;
    }
}