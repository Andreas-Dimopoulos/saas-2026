using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TodoApi.Data;
using TodoApi.Models;

namespace TodoApi.Tests;

public class DatabaseIsolationTests
{
    [Fact]
    public async Task SeparateFactories_DoNotShareData()
    {
        using var factoryOne = new TodoApiFactory();
        using var factoryTwo = new TodoApiFactory();

        using (var scope = factoryOne.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<TodoContext>();
            context.Todos.Add(new Todo
            {
                Title = "Only in factory one",
                CreatedBy = "alice",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }

        using (var scope = factoryOne.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<TodoContext>();
            Assert.Equal(1, await context.Todos.CountAsync());
        }

        using (var scope = factoryTwo.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<TodoContext>();
            Assert.Equal(0, await context.Todos.CountAsync());
        }
    }
}
