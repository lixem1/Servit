using System.ComponentModel.DataAnnotations;

namespace Servit.Api.Contracts.Account;

public class ChangePasswordRequest
{
    /// <summary>Not required for accounts that don't have a password yet (e.g. Google sign-in only).</summary>
    public string? CurrentPassword { get; set; }

    [Required, MinLength(8)]
    public required string NewPassword { get; set; }
}
