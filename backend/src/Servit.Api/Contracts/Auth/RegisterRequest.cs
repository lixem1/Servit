using System.ComponentModel.DataAnnotations;

namespace Servit.Api.Contracts.Auth;

public class RegisterRequest
{
    [Required]
    public required string FullName { get; set; }

    [Required, EmailAddress]
    public required string Email { get; set; }

    [Required, MinLength(8)]
    public required string Password { get; set; }

    /// <summary>"Customer" or "Provider" — see Servit.Domain.Constants.Roles.</summary>
    [Required]
    public required string Role { get; set; }
}
