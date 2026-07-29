using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Portal.Data;
using Portal.Models;

namespace Portal.Tests;

/// <summary>
/// Exercises the ConversationParticipant unique index directly through PortalContext,
/// bypassing ConversationsController entirely - same rationale as ContactConstraintTests:
/// a controller-level test only proves the controller's own dedup query works, not that
/// the database itself would still reject the row if that query were ever removed.
/// </summary>
public class ConversationConstraintTests
{
    [Fact]
    public async Task ConversationParticipants_RejectsDuplicateParticipant_ThroughDbContextDirectly()
    {
        using var factory = new PortalFactory();
        await AuthTestHelper.RegisterAndSignInAsync(factory, "alice@example.com");

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PortalContext>();
        var alice = await context.Users.FirstAsync(user => user.Email == "alice@example.com");

        var conversation = new Conversation { CreatedAt = DateTime.UtcNow };
        context.Conversations.Add(conversation);
        await context.SaveChangesAsync();

        context.ConversationParticipants.Add(new ConversationParticipant
        {
            ConversationId = conversation.Id,
            UserId = alice.Id,
            JoinedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        context.ConversationParticipants.Add(new ConversationParticipant
        {
            ConversationId = conversation.Id,
            UserId = alice.Id,
            JoinedAt = DateTime.UtcNow
        });
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }
}
