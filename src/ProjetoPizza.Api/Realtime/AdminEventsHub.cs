using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ProjetoPizza.Api.Realtime;

[Authorize(Policy = "AdminAccess")]
public sealed class AdminEventsHub : Hub;
