using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Portal.Tests;

public static partial class AuthTestHelper
{
    [GeneratedRegex("name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]*)\"")]
    private static partial Regex AntiforgeryTokenPattern();

    public static async Task<HttpClient> RegisterAndSignInAsync(
        PortalFactory factory,
        string email,
        string displayName = "Test User",
        string password = "Passw0rd!")
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var registerPage = await client.GetStringAsync("/Account/Register");
        var token = AntiforgeryTokenPattern().Match(registerPage).Groups[1].Value;

        var form = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Email"] = email,
            ["DisplayName"] = displayName,
            ["Password"] = password,
            ["ConfirmPassword"] = password
        };

        await client.PostAsync("/Account/Register", new FormUrlEncodedContent(form));

        return client;
    }

    public static async Task<string> GetAntiforgeryTokenAsync(HttpClient client, string url)
    {
        var page = await client.GetStringAsync(url);
        return AntiforgeryTokenPattern().Match(page).Groups[1].Value;
    }
}
