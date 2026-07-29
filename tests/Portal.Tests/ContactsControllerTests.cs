using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Portal.Data;
using Portal.Models;

namespace Portal.Tests;

public class ContactsControllerTests
{
    [Fact]
    public async Task Remove_ReturnsNotFound_WhenContactBelongsToDifferentUser()
    {
        using var factory = new PortalFactory();
        await AuthTestHelper.RegisterAndSignInAsync(factory, "alice@example.com");
        var bobClient = await AuthTestHelper.RegisterAndSignInAsync(factory, "bob@example.com");

        var contactId = await SeedContactAsync(factory, ownerEmail: "alice@example.com", contactEmail: "bob@example.com");
        var token = await AuthTestHelper.GetAntiforgeryTokenAsync(bobClient, "/Contacts/Browse");

        var response = await bobClient.PostAsync(
            $"/Contacts/Remove/{contactId}",
            new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = token }));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Add_RejectsSelfAdd_WithFriendlyMessage()
    {
        using var factory = new PortalFactory();
        var client = await AuthTestHelper.RegisterAndSignInAsync(factory, "alice@example.com");

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PortalContext>();
        var alice = await context.Users.FirstAsync(user => user.Email == "alice@example.com");

        var token = await AuthTestHelper.GetAntiforgeryTokenAsync(client, "/Contacts/Browse");
        var response = await client.PostAsync(
            "/Contacts/Add",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["contactUserId"] = alice.Id
            }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        var browsePage = await client.GetStringAsync("/Contacts/Browse");
        Assert.Contains("add yourself as a contact", browsePage);

        Assert.Empty(await context.Contacts.Where(contact => contact.OwnerId == alice.Id).ToListAsync());
    }

    [Fact]
    public async Task Add_RejectsDuplicateAdd_WithFriendlyMessage()
    {
        using var factory = new PortalFactory();
        var client = await AuthTestHelper.RegisterAndSignInAsync(factory, "alice@example.com");
        await AuthTestHelper.RegisterAndSignInAsync(factory, "bob@example.com");

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PortalContext>();
        var alice = await context.Users.FirstAsync(user => user.Email == "alice@example.com");
        var bob = await context.Users.FirstAsync(user => user.Email == "bob@example.com");

        context.Contacts.Add(new Contact { OwnerId = alice.Id, ContactUserId = bob.Id, CreatedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var token = await AuthTestHelper.GetAntiforgeryTokenAsync(client, "/Contacts/Browse");
        var response = await client.PostAsync(
            "/Contacts/Add",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["contactUserId"] = bob.Id
            }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        var browsePage = await client.GetStringAsync("/Contacts/Browse");
        Assert.Contains("already in your contacts", browsePage);

        var count = await context.Contacts.CountAsync(contact => contact.OwnerId == alice.Id && contact.ContactUserId == bob.Id);
        Assert.Equal(1, count);
    }

    private static async Task<int> SeedContactAsync(PortalFactory factory, string ownerEmail, string contactEmail)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PortalContext>();
        var owner = await context.Users.FirstAsync(user => user.Email == ownerEmail);
        var contactUser = await context.Users.FirstAsync(user => user.Email == contactEmail);

        var contact = new Contact { OwnerId = owner.Id, ContactUserId = contactUser.Id, CreatedAt = DateTime.UtcNow };
        context.Contacts.Add(contact);
        await context.SaveChangesAsync();

        return contact.Id;
    }
}
