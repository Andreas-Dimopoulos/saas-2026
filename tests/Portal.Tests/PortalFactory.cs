using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Portal.Data;

namespace Portal.Tests;

public sealed class PortalFactory : WebApplicationFactory<Program>
{
    public SqliteConnection Connection { get; } = new("Data Source=:memory:");

    public PortalFactory()
    {
        Connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.Single(d => d.ServiceType == typeof(DbContextOptions<PortalContext>));
            services.Remove(descriptor);

            services.AddDbContext<PortalContext>(options => options.UseSqlite(Connection));

            using var scope = services.BuildServiceProvider().CreateScope();
            scope.ServiceProvider.GetRequiredService<PortalContext>().Database.EnsureCreated();
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Connection.Dispose();
        }

        base.Dispose(disposing);
    }
}
