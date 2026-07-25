using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace TodoApi.Tests;

public static class AuthTestHelper
{
    public static async Task<string> SignupAndLoginAsync(HttpClient client, string email, string password = "Sup3rSecret1")
    {
        await client.PostAsJsonAsync("/signup", new { email, password });
        var loginResponse = await client.PostAsJsonAsync("/auth/login", new { email, password });
        var body = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("token").GetString()!;
    }

    public static async Task<HttpClient> AuthenticatedClientAsync(TodoApiFactory factory, string email, string password = "Sup3rSecret1")
    {
        var client = factory.CreateClient();
        var token = await SignupAndLoginAsync(client, email, password);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
