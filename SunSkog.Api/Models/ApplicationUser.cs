using Microsoft.AspNetCore.Identity;

namespace SunSkog.Api.Models;

public class ApplicationUser : IdentityUser  // <= žádný <Guid> !
{
    public string? FullName { get; set; }
}