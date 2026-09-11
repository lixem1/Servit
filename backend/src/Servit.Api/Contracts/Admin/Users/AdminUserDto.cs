namespace Servit.Api.Contracts.Admin.Users;

public class AdminUserDto
{
    public required Guid Id { get; set; }
    public required string Email { get; set; }
    public required string FullName { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
    public required bool IsSuspended { get; set; }
    public required IReadOnlyList<string> Roles { get; set; }
}
