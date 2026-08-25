using System.ComponentModel.DataAnnotations;

namespace Servit.Api.Contracts.Auth;

public class ForgotPasswordRequest
{
    [Required, EmailAddress]
    public required string Email { get; set; }
}
