namespace SunSkog.Api.Storage.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = default!;
    public string Name { get; set; } = default!;
    public Role Role { get; set; } = Role.Employee;
    public Guid? TeamId { get; set; }
    public Team? Team { get; set; }
    public bool IsActive { get; set; } = true;
}