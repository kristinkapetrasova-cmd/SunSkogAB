using SunSkog.Api.Models;

namespace SunSkog.Api.Storage.Entities;

public class Rate
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// Hodinová sazba (kr/h)
    /// </summary>
    public decimal HourRate { get; set; }
    
    /// <summary>
    /// Sazba za km (kr/km)
    /// </summary>
    public decimal KmRate { get; set; }
    
    /// <summary>
    /// Kusová sazba (kr/ks)
    /// </summary>
    public decimal PieceRate { get; set; }
    
    /// <summary>
    /// Platnost od (včetně)
    /// </summary>
    public DateOnly ValidFrom { get; set; }
    
    /// <summary>
    /// Platnost do (včetně), null = stále platná
    /// </summary>
    public DateOnly? ValidTo { get; set; }
    
    /// <summary>
    /// Je sazba aktivní
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Kdy byla sazba vytvořena/změněna
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// ID uživatele který sazbu vytvořil/změnil
    /// </summary>
    public string? ChangedByUserId { get; set; }
    
    /// <summary>
    /// Navigační vlastnost - uživatel který změnil
    /// </summary>
    public ApplicationUser? ChangedByUser { get; set; }
}