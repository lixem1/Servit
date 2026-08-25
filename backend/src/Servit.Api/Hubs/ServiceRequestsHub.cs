using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Servit.Api.Hubs;

[Authorize]
public class ServiceRequestsHub : Hub;
