using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace Portal.Tests;

public class BasicAuthenticationTests
{
    [Fact]
    public async Task Me_ReturnsUnauthorizedWithWwwAuthenticateHeader_WhenNoCredentials()
    {
        using var factory = new PortalFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains(response.Headers.WwwAuthenticate, header => header.Scheme == "Basic");
    }

    [Fact]
    public async Task Me_ReturnsUnauthorized_WhenPasswordIncorrect()
    {
        using var factory = new PortalFactory();
        await AuthTestHelper.RegisterAndSignInAsync(factory, "alice@example.com", password: "Passw0rd!");
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = BasicHeader("alice@example.com", "WrongPassword!");

        var response = await client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_ReturnsOk_WhenCredentialsValid()
    {
        using var factory = new PortalFactory();
        await AuthTestHelper.RegisterAndSignInAsync(factory, "alice@example.com", password: "Passw0rd!");
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = BasicHeader("alice@example.com", "Passw0rd!");

        var response = await client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Me_ReturnsUnauthorized_WhenAuthenticatedByCookieOnly()
    {
        using var factory = new PortalFactory();
        var client = await AuthTestHelper.RegisterAndSignInAsync(factory, "alice@example.com", password: "Passw0rd!");

        var response = await client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static AuthenticationHeaderValue BasicHeader(string email, string password)
    {
        var raw = Encoding.UTF8.GetBytes($"{email}:{password}");
        return new AuthenticationHeaderValue("Basic", Convert.ToBase64String(raw));
    }
}
