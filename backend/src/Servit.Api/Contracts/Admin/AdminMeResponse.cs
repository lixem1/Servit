namespace Servit.Api.Contracts.Admin;

public class AdminMeResponse
{
    public required Guid Id { get; set; }
    public required string Email { get; set; }
    public required string FullName { get; set; }
    public required IReadOnlyList<string> Roles { get; set; }
}
