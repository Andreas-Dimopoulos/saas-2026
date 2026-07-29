using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Portal.Hubs;

// Deliberately empty: Clients.User(id) (see NotificationService) already routes to
// every connection this user has open - multiple tabs, a popup, whatever - via the
// default IUserIdProvider, with no manual per-connection group bookkeeping needed.
// There are no client-invokable methods here at all, unlike ConversationHub - no
// method takes a caller-supplied id to join or act on, so there's no analogous
// cross-tenant surface to guard in the first place. The attack surface isn't
// defended here, it's absent.
[Authorize]
public class NotificationHub : Hub
{
}
