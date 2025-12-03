using SunSkog.Api.Models;

namespace SunSkog.Api.Storage.Entities;

public class Team
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Název týmu / party.</summary>
    public string Name { get; set; } = default!;

    /// <summary>Vedoucí party – vazba na AspNetUsers (ApplicationUser.Id je string).</summary>
    public string? LeadUserId { get; set; }
    public ApplicationUser? LeadUser { get; set; }

    /// <summary>Aktivní i historická členství.</summary>
    public List<TeamMembership> Members { get; set; } = new();
}