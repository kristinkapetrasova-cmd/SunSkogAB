namespace SunSkog.Api.Storage.Entities;

public enum MovementType
{
    In = 1,      // Příjem
    Out = 2,     // Výdej
    Adjust = 3   // Korekce
}

public class StockMovement
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ItemId { get; set; }
    public InventoryItem? Item { get; set; }
    public decimal Quantity { get; set; }           // kladné = příjem, záporné = výdej
    public DateTime At { get; set; } = DateTime.UtcNow;
    public string? Note { get; set; }               // poznámka k pohybu
    public MovementType Type { get; set; } = MovementType.In;
    
    // === NOVÉ: Výdej pro tým ===
    public Guid? TeamId { get; set; }
    public Team? Team { get; set; }
    
    // === Výdej pro osobu (existující) ===
    public string? UserId { get; set; }
}