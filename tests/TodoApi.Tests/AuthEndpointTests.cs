using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace TodoApi.Tests;

public class AuthEndpointTests
{
    [Fact]
    public async Task Signup_ReturnsCreatedWithoutPasswordHash()
    {
        using var factory = new TodoApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/signup", new { email = "alice@example.com", password = "Sup3rSecret!" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("id").GetInt32() > 0);
        Assert.Equal("alice@example.com", body.GetProperty("email").GetString());
        Assert.False(body.TryGetProperty("passwordHash", out _));
        Assert.False(body.TryGetProperty("password", out _));
    }

    [Fact]
    public async Task Signup_ReturnsConflict_WhenEmailAlreadyRegistered()
    {
        using var factory = new TodoApiFactory();
        var client = factory.CreateClient();
        await client.PostAsJsonAsync("/signup", new { email = "alice@example.com", password = "Sup3rSecret!" });

        var response = await client.PostAsJsonAsync("/signup", new { email = "alice@example.com", password = "AnotherPass1" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Signup_ReturnsBadRequestProblemDetails_WhenPasswordMissing()
    {
        using var factory = new TodoApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/signup", new { email = "alice@example.com" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Signup_ReturnsBadRequestProblemDetails_WhenPasswordWeak()
    {
        using var factory = new TodoApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/signup", new { email = "alice@example.com", password = "abc" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}
