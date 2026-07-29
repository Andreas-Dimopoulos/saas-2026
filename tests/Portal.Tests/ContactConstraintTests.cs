using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Portal.Data;
using Portal.Models;

namespace Portal.Tests;

/// <summary>
/// Exercises the database constraints directly through PortalContext, bypassing
/// ContactsController entirely. A controller-level test only proves the controller's
/// `if` checks work - it would still pass if the CHECK constraint or unique index were
/// removed from the schema. These prove the database itself rejects the row.
/// </summary>
public class ContactConstraintTests
{
    [Fact]
    public async Task Contacts_RejectsDuplicatePair_ThroughDbContextDirectly()
    {
        using var factory = new PortalFactory();
        await AuthTestHelper.RegisterAndSignInAsync(factory, "alice@example.com");
        await AuthTestHelper.RegisterAndSignInAsync(factory, "bob@example.com");

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PortalContext>();
        var alice = await context.Users.FirstAsync(user => user.Email == "alice@example.com");
        var bob = await context.Users.FirstAsync(user => user.Email == "bob@example.com");

        context.Contacts.Add(new Contact { OwnerId = alice.Id, ContactUserId = bob.Id, CreatedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();

        context.Contacts.Add(new Contact { OwnerId = alice.Id, ContactUserId = bob.Id, CreatedAt = DateTime.UtcNow });
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Contacts_RejectsSelfContact_ThroughDbContextDirectly()
    {
        using var factory = new PortalFactory();
        await AuthTestHelper.RegisterAndSignInAsync(factory, "alice@example.com");

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PortalContext>();
        var alice = await context.Users.FirstAsync(user => user.Email == "alice@example.com");

        context.Contacts.Add(new Contact { OwnerId = alice.Id, ContactUserId = alice.Id, CreatedAt = DateTime.UtcNow });
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }
}
