namespace Servit.Api.Contracts.Auth;

public class GoogleAuthRequest
{
    public required string IdToken { get; set; }
    public string? Role { get; set; }
}
