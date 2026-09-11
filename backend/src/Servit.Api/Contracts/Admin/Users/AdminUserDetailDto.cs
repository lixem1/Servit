namespace Servit.Api.Contracts.Admin.Users;

// 360 view of a user for the admin panel.
public class AdminUserDetailDto
{
    public required Guid Id { get; set; }
    public required string Email { get; set; }
    public required string FullName { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
    public required bool IsSuspended { get; set; }
    public required IReadOnlyList<string> Roles { get; set; }

    public AdminUserProviderDto? Provider { get; set; }

    public required int TotalRequests { get; set; }
    public required int CompletedRequests { get; set; }
    public required int CancelledRequests { get; set; }

    public required IReadOnlyList<AdminUserRequestDto> RecentRequests { get; set; }
    public required IReadOnlyList<AdminUserReviewDto> ReviewsGiven { get; set; }
    public required IReadOnlyList<AdminUserReviewDto> ReviewsReceived { get; set; }
}

public class AdminUserProviderDto
{
    public required Guid Id { get; set; }
    public string? Bio { get; set; }
    public required double AverageRating { get; set; }
    public required int RatingCount { get; set; }
    public required IReadOnlyList<string> CategoryNames { get; set; }
}

public class AdminUserRequestDto
{
    public required Guid Id { get; set; }
    public required string CategoryName { get; set; }
    public required string Status { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
    public required int ResponseCount { get; set; }
}

public class AdminUserReviewDto
{
    public required Guid Id { get; set; }
    public required string CounterpartName { get; set; }
    public required int Rating { get; set; }
    public string? Comment { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
}
