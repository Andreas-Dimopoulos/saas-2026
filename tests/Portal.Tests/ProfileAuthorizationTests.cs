using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Portal.Tests;

public class ProfileAuthorizationTests
{
    [Fact]
    public async Task Profile_RedirectsToLogin_WhenAnonymous()
    {
        using var factory = new PortalFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/Account/Profile");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Profile_ReturnsOk_WhenAuthenticated()
    {
        using var factory = new PortalFactory();
        var client = await AuthTestHelper.RegisterAndSignInAsync(factory, "alice@example.com");

        var response = await client.GetAsync("/Account/Profile");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
