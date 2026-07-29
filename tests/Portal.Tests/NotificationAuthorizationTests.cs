using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Portal.Data;
using Portal.Models;

namespace Portal.Tests;

public class NotificationAuthorizationTests
{
    [Fact]
    public async Task MarkAsRead_ReturnsNotFound_WhenNotificationBelongsToDifferentUser()
    {
        using var factory = new PortalFactory();
        var notificationId = await SeedNotificationAsync(factory, recipientEmail: "alice@example.com");
        var bobClient = await AuthTestHelper.RegisterAndSignInAsync(factory, "bob@example.com");
        var token = await AuthTestHelper.GetAntiforgeryTokenAsync(bobClient, "/Notifications/Index");

        var response = await bobClient.PostAsync(
            $"/Notifications/MarkAsRead/{notificationId}",
            new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = token }));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PortalContext>();
        var notification = await context.Notifications.FirstAsync(n => n.Id == notificationId);
        Assert.False(notification.IsRead);
    }

    [Fact]
    public async Task Index_DoesNotIncludeAnotherUsersNotifications()
    {
        using var factory = new PortalFactory();
        await SeedNotificationAsync(factory, recipientEmail: "alice@example.com", text: "AlicePrivateNotificationMarker");
        var bobClient = await AuthTestHelper.RegisterAndSignInAsync(factory, "bob@example.com");

        var page = await bobClient.GetStringAsync("/Notifications/Index");

        Assert.DoesNotContain("AlicePrivateNotificationMarker", page);
    }

    private static async Task<int> SeedNotificationAsync(
        PortalFactory factory,
        string recipientEmail,
        string text = "You have a notification")
    {
        await AuthTestHelper.RegisterAndSignInAsync(factory, recipientEmail);

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PortalContext>();
        var recipient = await context.Users.FirstAsync(user => user.Email == recipientEmail);

        var notification = new Notification
        {
            RecipientId = recipient.Id,
            Kind = NotificationKind.NewContact,
            Text = text,
            CreatedAt = DateTime.UtcNow,
            IsRead = false
        };

        context.Notifications.Add(notification);
        await context.SaveChangesAsync();

        return notification.Id;
    }
}
