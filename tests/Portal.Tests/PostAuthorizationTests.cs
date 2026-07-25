using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Portal.Data;
using Portal.Models;

namespace Portal.Tests;

public class PostAuthorizationTests
{
    [Fact]
    public async Task Edit_ReturnsNotFound_WhenPostBelongsToDifferentUser()
    {
        using var factory = new PortalFactory();
        var postId = await SeedPostAsync(factory, "alice@example.com", "Alice's post");
        var client = await AuthTestHelper.RegisterAndSignInAsync(factory, "bob@example.com");
        var token = await AuthTestHelper.GetAntiforgeryTokenAsync(client, "/Posts/Create");

        var response = await client.PostAsync(
            $"/Posts/Edit/{postId}",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["Title"] = "Hijacked",
                ["Body"] = "Hijacked body",
                ["Category"] = nameof(PostCategory.General)
            }));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ReturnsNotFound_WhenPostBelongsToDifferentUser()
    {
        using var factory = new PortalFactory();
        var postId = await SeedPostAsync(factory, "alice@example.com", "Alice's post");
        var client = await AuthTestHelper.RegisterAndSignInAsync(factory, "bob@example.com");
        var token = await AuthTestHelper.GetAntiforgeryTokenAsync(client, "/Posts/Create");

        var response = await client.PostAsync(
            $"/Posts/Delete/{postId}",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token
            }));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static async Task<int> SeedPostAsync(PortalFactory factory, string authorEmail, string title)
    {
        await AuthTestHelper.RegisterAndSignInAsync(factory, authorEmail);

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PortalContext>();
        var author = await context.Users.FirstAsync(user => user.Email == authorEmail);

        var now = DateTime.UtcNow;
        var post = new Post
        {
            Title = title,
            Body = "Body",
            Category = PostCategory.General,
            AuthorId = author.Id,
            CreatedAt = now,
            UpdatedAt = now
        };

        context.Posts.Add(post);
        await context.SaveChangesAsync();

        return post.Id;
    }
}
