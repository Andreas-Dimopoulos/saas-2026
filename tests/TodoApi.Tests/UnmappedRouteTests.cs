using System.Net;

namespace TodoApi.Tests;

public class UnmappedRouteTests : IDisposable
{
    private readonly TodoApiFactory _factory = new();

    [Fact]
    public async Task UnmappedRoute_ReturnsNotFound()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/this-route-does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    public void Dispose() => _factory.Dispose();
}
