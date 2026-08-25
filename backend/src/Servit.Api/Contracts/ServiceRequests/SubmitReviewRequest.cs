using System.ComponentModel.DataAnnotations;

namespace Servit.Api.Contracts.ServiceRequests;

public class SubmitReviewRequest
{
    [Required]
    [Range(1, 5)]
    public required int Rating { get; set; }

    public string? Comment { get; set; }
}
