using System.ComponentModel.DataAnnotations;

namespace Servit.Api.Contracts.Providers;

public class UpdateProviderCategoriesRequest
{
    [Required]
    public required List<int> CategoryIds { get; set; }
}
