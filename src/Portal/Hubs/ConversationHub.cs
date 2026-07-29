using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Portal.Data;
using Portal.Models;
using Portal.Services;

namespace Portal.Hubs;

[Authorize]
public class ConversationHub(PortalContext context, UserManager<PortalUser> userManager, NotificationService notifications) : Hub
{
    // [Authorize] only proves the caller is signed in - it says nothing about which
    // conversations they may join. Without this check, any authenticated user could
    // call JoinConversation with any id and start receiving every message broadcast
    // to that group, regardless of whether they're a participant. This is the same
    // cross-tenant mistake ItemsController guards against, just easier to miss here
    // because there's no [Authorize]-shaped attribute that catches it for you.
    public async Task JoinConversation(int conversationId)
    {
        var userId = userManager.GetUserId(Context.User!)!;

        if (!await context.IsParticipantAsync(conversationId, userId))
        {
            throw new HubException("Not a participant in this conversation.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(conversationId));
    }

    public async Task SendMessage(int conversationId, string body)
    {
        var userId = userManager.GetUserId(Context.User!)!;

        // Checked again here, independently of JoinConversation - a client could call
        // SendMessage for a conversation it never joined, so this method can't rely on
        // group membership as a proxy for authorization.
        if (!await context.IsParticipantAsync(conversationId, userId))
        {
            throw new HubException("Not a participant in this conversation.");
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            throw new HubException("Message body cannot be empty.");
        }

        // The sender is Context.User, resolved server-side via the authenticated
        // connection - never a value the client sends. A client invoking this method
        // has no way to claim to be sending as anyone else.
        var sender = await userManager.GetUserAsync(Context.User!)
            ?? throw new HubException("Unknown sender.");

        var message = new Message
        {
            ConversationId = conversationId,
            SenderId = userId,
            Body = body,
            SentAt = DateTime.UtcNow
        };

        // Persisted before it's broadcast, so a page reload shows exactly the history
        // a live listener already saw - the hub is additive on top of storage that
        // works the same way the plain-HTTP SendMessage action already does.
        context.Messages.Add(message);
        await context.SaveChangesAsync();

        // Broadcast to the whole group, including the sender's own other connections -
        // there's no separate "echo to caller" path. Clients render only from this
        // event, never by optimistically appending on submit, so the sender doesn't
        // see their own message twice.
        await Clients.Group(GroupName(conversationId))
            .SendAsync("ReceiveMessage", sender.DisplayName, userId, message.Body, message.SentAt);

        await notifications.NotifyNewMessageAsync(conversationId, userId, sender.DisplayName);
    }

    private static string GroupName(int conversationId) => $"conversation-{conversationId}";
}
