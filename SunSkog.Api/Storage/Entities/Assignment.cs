using SunSkog.Api.Models;

namespace SunSkog.Api.Storage.Entities;

public class Assignment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ItemId { get; set; }
    public InventoryItem? Item { get; set; }
    
    // Přiřazení osobě
    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }
    
    // Přiřazení týmu (NOVÉ)
    public Guid? TeamId { get; set; }
    public Team? Team { get; set; }
    
    public int Quantity { get; set; } = 1;
    public DateTime AssignedAt { get; set; }
    public DateTime? ReturnedAt { get; set; }
    public string? Note { get; set; }
}