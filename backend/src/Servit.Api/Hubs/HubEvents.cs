namespace Servit.Api.Hubs;

public static class HubEvents
{
    public const string ServiceRequestCreated = "ServiceRequestCreated";
    public const string ResponseReceived = "ResponseReceived";
    public const string ResponseAccepted = "ResponseAccepted";
    public const string ResponseDeclined = "ResponseDeclined";
    public const string ServiceRequestUnavailable = "ServiceRequestUnavailable";
    public const string ServiceRequestCancelled = "ServiceRequestCancelled";
}
