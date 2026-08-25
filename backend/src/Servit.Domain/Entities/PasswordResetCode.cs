namespace Servit.Domain.Entities;

public class PasswordResetCode
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public required string CodeHash { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}
