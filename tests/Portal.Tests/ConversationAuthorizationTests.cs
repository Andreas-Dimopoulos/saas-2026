using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Portal.Data;
using Portal.Models;

namespace Portal.Tests;

public class ConversationAuthorizationTests
{
    [Fact]
    public async Task Show_ReturnsNotFound_WhenRequesterIsNotAParticipant()
    {
        using var factory = new PortalFactory();
        var conversationId = await SeedConversationAsync(factory, "alice@example.com", "bob@example.com");
        var carolClient = await AuthTestHelper.RegisterAndSignInAsync(factory, "carol@example.com");

        var response = await carolClient.GetAsync($"/Conversations/Show/{conversationId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SendMessage_ReturnsNotFound_AndPersistsNothing_WhenRequesterIsNotAParticipant()
    {
        using var factory = new PortalFactory();
        var conversationId = await SeedConversationAsync(factory, "alice@example.com", "bob@example.com");
        var carolClient = await AuthTestHelper.RegisterAndSignInAsync(factory, "carol@example.com");
        var token = await AuthTestHelper.GetAntiforgeryTokenAsync(carolClient, "/Conversations/New");

        var response = await carolClient.PostAsync(
            $"/Conversations/SendMessage/{conversationId}",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["Body"] = "Hijacked message"
            }));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PortalContext>();
        Assert.Empty(await context.Messages.Where(message => message.ConversationId == conversationId).ToListAsync());
    }

    private static async Task<int> SeedConversationAsync(PortalFactory factory, string aliceEmail, string bobEmail)
    {
        await AuthTestHelper.RegisterAndSignInAsync(factory, aliceEmail);
        await AuthTestHelper.RegisterAndSignInAsync(factory, bobEmail);

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PortalContext>();
        var alice = await context.Users.FirstAsync(user => user.Email == aliceEmail);
        var bob = await context.Users.FirstAsync(user => user.Email == bobEmail);

        var now = DateTime.UtcNow;
        var conversation = new Conversation { CreatedAt = now };
        conversation.Participants.Add(new ConversationParticipant { UserId = alice.Id, JoinedAt = now });
        conversation.Participants.Add(new ConversationParticipant { UserId = bob.Id, JoinedAt = now });

        context.Conversations.Add(conversation);
        await context.SaveChangesAsync();

        return conversation.Id;
    }
}
