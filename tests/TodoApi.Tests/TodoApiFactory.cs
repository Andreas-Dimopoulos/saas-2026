using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;

namespace TodoApi.Tests;

public sealed class TodoApiFactory : WebApplicationFactory<Program>
{
    public SqliteConnection Connection { get; } = new("Data Source=:memory:");

    public TodoApiFactory()
    {
        Connection.Open();
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
