using System.ComponentModel.DataAnnotations;

namespace Servit.Api.Contracts.ServiceRequests;

public class CreateServiceRequestRequest
{
    [Required]
    public required int CategoryId { get; set; }

    [Required]
    public required string Description { get; set; }

    public required double Lat { get; set; }
    public required double Lng { get; set; }

    public List<IFormFile>? Photos { get; set; }
    public IFormFile? Video { get; set; }
    public List<IFormFile>? Audios { get; set; }
}
