using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TodoApi.Data;

namespace TodoApi.Tests;

public sealed class TodoApiFactory : WebApplicationFactory<Program>
{
    private const string TestJwtSigningKey = "2riaNiCrmrhUgZZrOrjWaD+3LHcB9Xe8iDhMdzPUhXQ=";

    public SqliteConnection Connection { get; } = new("Data Source=:memory:");

    public TodoApiFactory()
    {
        // ConfigureWebHost's config runs too late for Program.cs's eager Jwt:SigningKey read; env vars don't.
        Environment.SetEnvironmentVariable("Jwt__SigningKey", TestJwtSigningKey);
        Connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.Single(d => d.ServiceType == typeof(DbContextOptions<TodoContext>));
            services.Remove(descriptor);

            services.AddDbContext<TodoContext>(options => options.UseSqlite(Connection));

            using var scope = services.BuildServiceProvider().CreateScope();
            scope.ServiceProvider.GetRequiredService<TodoContext>().Database.EnsureCreated();
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
