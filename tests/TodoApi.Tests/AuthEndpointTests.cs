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

    [Fact]
    public async Task Login_ReturnsTokenWithExpiry_WhenCredentialsValid()
    {
        using var factory = new TodoApiFactory();
        var client = factory.CreateClient();
        await client.PostAsJsonAsync("/signup", new { email = "alice@example.com", password = "Sup3rSecret1" });

        var response = await client.PostAsJsonAsync("/auth/login", new { email = "alice@example.com", password = "Sup3rSecret1" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var token = body.GetProperty("token").GetString();
        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.True(body.GetProperty("expiresAt").GetDateTime() > DateTime.UtcNow);
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenPasswordWrong()
    {
        using var factory = new TodoApiFactory();
        var client = factory.CreateClient();
        await client.PostAsJsonAsync("/signup", new { email = "alice@example.com", password = "Sup3rSecret1" });

        var response = await client.PostAsJsonAsync("/auth/login", new { email = "alice@example.com", password = "WrongPassword1" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenEmailNotRegistered()
    {
        using var factory = new TodoApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/login", new { email = "nobody@example.com", password = "Sup3rSecret1" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_ReturnsUnauthorized_WithoutToken()
    {
        using var factory = new TodoApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/auth/logout");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_ReturnsNoContent_WhenAuthenticated()
    {
        using var factory = new TodoApiFactory();
        var client = await AuthTestHelper.AuthenticatedClientAsync(factory, "alice@example.com");

        var response = await client.GetAsync("/auth/logout");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Logout_RevokesToken_SoSecondUseReturnsUnauthorized()
    {
        using var factory = new TodoApiFactory();
        var client = await AuthTestHelper.AuthenticatedClientAsync(factory, "alice@example.com");

        var firstLogout = await client.GetAsync("/auth/logout");
        Assert.Equal(HttpStatusCode.NoContent, firstLogout.StatusCode);

        var secondLogout = await client.GetAsync("/auth/logout");
        Assert.Equal(HttpStatusCode.Unauthorized, secondLogout.StatusCode);
    }
}
