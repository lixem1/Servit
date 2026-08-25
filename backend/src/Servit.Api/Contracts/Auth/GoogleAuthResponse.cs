namespace Servit.Api.Contracts.Auth;

public class GoogleAuthResponse
{
    public required bool RequiresRole { get; set; }
    public string? Email { get; set; }
    public string? FullName { get; set; }
    public AuthResponse? Auth { get; set; }
}
