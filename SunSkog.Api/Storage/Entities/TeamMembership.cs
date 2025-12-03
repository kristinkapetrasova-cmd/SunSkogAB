using SunSkog.Api.Models; // kvůli ApplicationUser
using System;

namespace SunSkog.Api.Storage.Entities
{
    public class TeamMembership
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid TeamId { get; set; }                // FK → Team.Id (Guid)
        public Team? Team { get; set; }                 // navigace (volitelně)

        public string UserId { get; set; } = default!;  // FK → AspNetUsers.Id (string)
        // navigaci na uživatele mít nemusíš; pokud chceš:
        // public ApplicationUser? User { get; set; }

        public DateOnly FromDate { get; set; }
        public DateOnly? ToDate { get; set; }

        public string Role { get; set; } = "Member";    // "Member" | "Lead"
    }
}