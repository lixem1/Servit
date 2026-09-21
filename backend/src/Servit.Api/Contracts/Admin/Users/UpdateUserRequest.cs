using System.ComponentModel.DataAnnotations;

namespace Servit.Api.Contracts.Admin.Users;

public class UpdateUserRequest
{
    [Required]
    [MaxLength(150)]
    public required string FullName { get; set; }

    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public required string Email { get; set; }
}
