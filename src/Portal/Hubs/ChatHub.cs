using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Portal.Hubs;

// Spike only - proving SignalR connectivity and hub identity before any real data
// model exists. [Authorize] here uses the same cookie scheme as the rest of the
// site (Identity's default), so an unauthenticated negotiate request never reaches
// OnConnectedAsync at all.
[Authorize]
public class ChatHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var name = Context.User?.Identity?.Name ?? "(no name claim)";
        await Clients.Caller.SendAsync("ReceiveMessage", "system", $"Connected as {name}");
        await base.OnConnectedAsync();
    }

    public async Task SendMessage(string message)
    {
        var name = Context.User?.Identity?.Name ?? "(no name claim)";
        await Clients.All.SendAsync("ReceiveMessage", name, message);
    }
}
