using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ProjetoPizza.Api.Realtime;

[Authorize(Policy = "AdminOrOperationsAccess")]
public sealed class AdminEventsHub : Hub;
