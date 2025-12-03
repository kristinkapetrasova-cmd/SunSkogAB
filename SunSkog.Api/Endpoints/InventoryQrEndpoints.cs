using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using SunSkog.Api.Data;

namespace SunSkog.Api.Endpoints;

public static class InventoryQrEndpoints
{
    public static IEndpointRouteBuilder MapInventoryQrEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/inventory")
                       .WithTags("Inventory")
                       .RequireAuthorization("CanUseInventory");

        // GET /api/inventory/{id}/qrcode  -> PNG obrázek
        group.MapGet("/{id:guid}/qrcode", GetQrCode)
             .WithName("Inventory_QR")
             .Produces(200, contentType: "image/png")
             .WithOpenApi();

        return app;
    }

    private static async Task<IResult> GetQrCode(
        ApplicationDbContext db,
        HttpContext http,
        Guid id,
        IConfiguration config,
        CancellationToken ct)
    {
        var item = await db.InventoryItems
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (item is null)
            return Results.NotFound(new { error = "Item not found" });

        // Získat base URL z konfigurace nebo z requestu
        var baseUrl = config["App:FrontendUrl"] 
            ?? config["FrontendUrl"] 
            ?? $"{http.Request.Scheme}://{http.Request.Host}";

        // QR kód obsahuje URL odkaz na detail položky
        // Formát: https://sunskog.example.com/app/warehouse?item=GUID
        var payload = $"{baseUrl}/app/warehouse?item={item.Id}";

        using var generator = new QRCodeGenerator();
        var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.M, true, true, QRCodeGenerator.EciMode.Default);
        var pngQr = new PngByteQRCode(data);
        var bytes = pngQr.GetGraphic(pixelsPerModule: 6);

        return Results.File(bytes, "image/png", $"{item.Name}_qr.png");
    }
}
