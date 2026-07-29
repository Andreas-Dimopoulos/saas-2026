using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Portal.Data;
using Portal.Hubs;
using Portal.Models;

namespace Portal.Services;

public class NotificationService(PortalContext context, IHubContext<NotificationHub> notificationHub)
{
    public async Task NotifyNewMessageAsync(int conversationId, string senderId, string senderDisplayName)
    {
        var recipientIds = await context.ConversationParticipants
            .Where(participant => participant.ConversationId == conversationId && participant.UserId != senderId)
            .Select(participant => participant.UserId)
            .ToListAsync();

        if (recipientIds.Count == 0)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var text = $"{senderDisplayName} sent you a message.";
        var toPush = new List<Notification>();

        foreach (var recipientId in recipientIds)
        {
            // Collapse: twenty messages in one conversation should read as one unread
            // notification, not twenty, so a demo of five messages doesn't show a badge
            // reading "5" for one thread. An existing *unread* NewMessage notification
            // for this (recipient, conversation) pair is updated in place instead of
            // duplicated. Once it's been read, the next message starts a fresh one -
            // "still unread" and "caught up, then something new" are different
            // situations and both deserve to be visible as such.
            var existing = await context.Notifications.FirstOrDefaultAsync(notification =>
                notification.RecipientId == recipientId &&
                notification.ConversationId == conversationId &&
                notification.Kind == NotificationKind.NewMessage &&
                !notification.IsRead);

            if (existing is not null)
            {
                existing.Text = text;
                existing.ActorId = senderId;
                existing.CreatedAt = now;
                toPush.Add(existing);
            }
            else
            {
                var notification = new Notification
                {
                    RecipientId = recipientId,
                    ActorId = senderId,
                    Kind = NotificationKind.NewMessage,
                    Text = text,
                    ConversationId = conversationId,
                    CreatedAt = now,
                    IsRead = false
                };
                context.Notifications.Add(notification);
                toPush.Add(notification);
            }
        }

        await context.SaveChangesAsync();

        foreach (var notification in toPush)
        {
            await notificationHub.Clients.User(notification.RecipientId)
                .SendAsync("ReceiveNotification", notification.Id, notification.Text, notification.CreatedAt);
        }
    }

    public async Task NotifyNewContactAsync(string ownerId, string ownerDisplayName, string contactUserId)
    {
        var notification = new Notification
        {
            RecipientId = contactUserId,
            ActorId = ownerId,
            Kind = NotificationKind.NewContact,
            Text = $"{ownerDisplayName} added you as a contact.",
            ConversationId = null,
            CreatedAt = DateTime.UtcNow,
            IsRead = false
        };

        context.Notifications.Add(notification);
        await context.SaveChangesAsync();

        await notificationHub.Clients.User(contactUserId)
            .SendAsync("ReceiveNotification", notification.Id, notification.Text, notification.CreatedAt);
    }
}
