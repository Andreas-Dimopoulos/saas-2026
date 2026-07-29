using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Portal.Models;

namespace Portal.Tests;

public class ExternalLoginTests
{
    [Fact]
    public async Task ExternalLoginCallback_Rejects_WhenEmailAlreadyRegisteredLocally()
    {
        using var factory = new PortalFactory();
        await AuthTestHelper.RegisterAndSignInAsync(factory, "alice@example.com", password: "Passw0rd!");
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        await client.GetAsync("/test/fake-external-login?providerKey=google-1&email=alice@example.com&name=Alice&verified=true");
        var response = await client.GetAsync("/Account/ExternalLoginCallback");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("account already exists", body, StringComparison.OrdinalIgnoreCase);

        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<PortalUser>>();
        var alice = await userManager.FindByEmailAsync("alice@example.com");
        var logins = await userManager.GetLoginsAsync(alice!);
        Assert.Empty(logins);
    }

    [Fact]
    public async Task ExternalLoginCallback_Rejects_WhenEmailMissing()
    {
        using var factory = new PortalFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        await client.GetAsync("/test/fake-external-login?providerKey=google-2&name=No+Email&verified=true");
        var response = await client.GetAsync("/Account/ExternalLoginCallback");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("did not share an email address", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExternalLoginCallback_Rejects_WhenEmailNotVerified()
    {
        using var factory = new PortalFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        await client.GetAsync("/test/fake-external-login?providerKey=google-3&email=new@example.com&name=New+Person&verified=false");
        var response = await client.GetAsync("/Account/ExternalLoginCallback");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("has not verified this email", body, StringComparison.OrdinalIgnoreCase);

        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<PortalUser>>();
        Assert.Null(await userManager.FindByEmailAsync("new@example.com"));
    }

    [Fact]
    public async Task ExternalLoginCallback_CreatesUserAndSignsIn_WhenNewVerifiedEmail()
    {
        using var factory = new PortalFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        await client.GetAsync("/test/fake-external-login?providerKey=google-4&email=new@example.com&name=New+Person&verified=true");
        var callbackResponse = await client.GetAsync("/Account/ExternalLoginCallback");

        Assert.Equal(HttpStatusCode.Redirect, callbackResponse.StatusCode);

        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<PortalUser>>();
        var created = await userManager.FindByEmailAsync("new@example.com");
        Assert.NotNull(created);
        Assert.Equal("New Person", created.DisplayName);
        var logins = await userManager.GetLoginsAsync(created);
        Assert.Contains(logins, login => login.LoginProvider == "Google" && login.ProviderKey == "google-4");

        var profileResponse = await client.GetAsync("/Account/Profile");
        Assert.Equal(HttpStatusCode.OK, profileResponse.StatusCode);
    }
}
