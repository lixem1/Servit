namespace Servit.Api.Contracts.Account;

public class AccountProfileResponse
{
    public required string FullName { get; set; }
    public required string Email { get; set; }
    public required string Role { get; set; }
    public required bool HasPassword { get; set; }
}
