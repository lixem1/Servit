using System.ComponentModel.DataAnnotations;

namespace Servit.Api.Contracts.Admin.ServiceRequests;

public class UpdateRequestStatusRequest
{
    [Required]
    public required string Status { get; set; }
}
