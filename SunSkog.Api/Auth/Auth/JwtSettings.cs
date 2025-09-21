namespace SunSkog.Api.Auth
{
    public class JwtSettings
    {
        public string Issuer { get; set; } = "SunSkog";
        public string Audience { get; set; } = "SunSkogUsers";
        public string Key { get; set; } = "change-me-please-super-secret";
    }
    public static class RoleNames
    {
        public const string Employee = "Employee";
        public const string CrewLead = "CrewLead";
        public const string Accountant = "Accountant";
        public const string Management = "Management";
        public const string SuperAdmin = "SuperAdmin";
    }
}
