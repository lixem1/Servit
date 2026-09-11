using System.ComponentModel.DataAnnotations;

namespace Servit.Api.Contracts.Admin.Users;

public class ChangeRoleRequest
{
    [Required]
    public required string Role { get; set; }
}
