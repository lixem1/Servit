namespace Servit.Api.Contracts.Providers;

public class UpdateProviderLocationRequest
{
    public required double Lat { get; set; }
    public required double Lng { get; set; }
}
