using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SunSkog.Api.Models;

public class InventoryItem
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = default!;

    [MaxLength(100)]
    public string? Code { get; set; }  // interní kód / QR

    [MaxLength(50)]
    public string Unit { get; set; } = "ks";

    // aktuální stav
    [Column(TypeName = "decimal(18,2)")]
    public decimal Quantity { get; set; }

    // minimální množství pro upozornění
    [Column(TypeName = "decimal(18,2)")]
    public decimal? MinQuantity { get; set; }

    [MaxLength(200)]
    public string? Location { get; set; } // sklad / regál / auto

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<InventoryMovement> Movements { get; set; } = new List<InventoryMovement>();
}