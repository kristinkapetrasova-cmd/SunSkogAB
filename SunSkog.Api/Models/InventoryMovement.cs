using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SunSkog.Api.Models;

public class InventoryMovement
{
    public Guid Id { get; set; }

    [Required]
    public Guid ItemId { get; set; }

    public InventoryItem Item { get; set; } = default!;

    // In / Out
    [Required]
    [MaxLength(20)]
    public string Type { get; set; } = default!; // "In" nebo "Out"

    [Column(TypeName = "decimal(18,2)")]
    public decimal Quantity { get; set; }

    public DateTime When { get; set; } = DateTime.UtcNow;

    [MaxLength(500)]
    public string? Note { get; set; }

    // volitelné – komu to bylo vydáno
    public string? EmployeeId { get; set; }
    public ApplicationUser? Employee { get; set; }
}