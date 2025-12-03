namespace SunSkog.Api.Storage.Entities;

public class InventoryItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = default!;
    public string? SKU { get; set; }           // interní kód
    public string? SerialNumber { get; set; }  // sériové číslo / QR
    public int MinStock { get; set; } = 0;     // změněno na int
    public bool IsActive { get; set; } = true;
    
    // === Kategorie ===
    public Guid? CategoryId { get; set; }
    public Category? Category { get; set; }
    
    // === Velikost (volitelné - hlavně pro oblečení) ===
    public string? Size { get; set; }  // např. "S", "M", "L", "XL" nebo "8", "9", "10", "11"
    
    // === Druh/Typ (volitelné - např. "Sázení", "Řezačské - Tegera") ===
    public string? ItemType { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}