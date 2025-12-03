namespace SunSkog.Api.Storage.Entities;

public class Category
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = default!;       // "Oblečení", "Sázení", ...
    public string? NameEn { get; set; }                // anglický název
    public bool HasSizes { get; set; } = false;        // zda kategorie používá velikosti
    public bool HasItemTypes { get; set; } = false;    // zda kategorie používá druhy (např. "Sázení", "Řezačské")
    public int SortOrder { get; set; } = 0;            // pořadí zobrazení
    public bool IsActive { get; set; } = true;
    
    public ICollection<InventoryItem> Items { get; set; } = new List<InventoryItem>();
}