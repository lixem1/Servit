using System.ComponentModel.DataAnnotations;

namespace Servit.Api.Contracts.Account;

public class UpdateProfileRequest
{
    [Required]
    public required string FullName { get; set; }
}
