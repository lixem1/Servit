namespace Servit.Api.Contracts.Auth;

public class AuthResponse
{
    public required string Token { get; set; }
    public required DateTimeOffset ExpiresAt { get; set; }
    public required Guid UserId { get; set; }
    public required string FullName { get; set; }
    public required string Role { get; set; }
}
