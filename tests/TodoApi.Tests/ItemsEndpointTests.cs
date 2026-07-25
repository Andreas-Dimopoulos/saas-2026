using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TodoApi.Data;
using TodoApi.Models;

namespace TodoApi.Tests;

public class ItemsEndpointTests
{
    [Fact]
    public async Task GetItem_ReturnsOkWithMatchingItem()
    {
        using var factory = new TodoApiFactory();
        var (todoId, itemId) = await SeedTodoWithItemAsync(factory, "Groceries", "Milk");
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/todos/{todoId}/items/{itemId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(itemId, body.GetProperty("id").GetInt32());
        Assert.Equal("Milk", body.GetProperty("name").GetString());
        Assert.False(body.GetProperty("done").GetBoolean());
    }

    [Fact]
    public async Task GetItem_ReturnsNotFound_WhenTodoMissing()
    {
        using var factory = new TodoApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/todos/999/items/1");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetItem_ReturnsNotFound_WhenItemBelongsToDifferentTodo()
    {
        using var factory = new TodoApiFactory();
        var (_, itemId) = await SeedTodoWithItemAsync(factory, "Groceries", "Milk");
        var otherTodoId = await SeedTodoAsync(factory, "Work");
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/todos/{otherTodoId}/items/{itemId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetItem_ReturnsNotFound_WhenItemMissing()
    {
        using var factory = new TodoApiFactory();
        var todoId = await SeedTodoAsync(factory, "Groceries");
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/todos/{todoId}/items/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    private static async Task<int> SeedTodoAsync(TodoApiFactory factory, string title)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TodoContext>();
        var now = DateTime.UtcNow;
        var todo = new Todo { Title = title, CreatedBy = "alice", CreatedAt = now, UpdatedAt = now };
        context.Todos.Add(todo);
        await context.SaveChangesAsync();
        return todo.Id;
    }

    private static async Task<(int TodoId, int ItemId)> SeedTodoWithItemAsync(TodoApiFactory factory, string todoTitle, string itemName)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TodoContext>();
        var now = DateTime.UtcNow;
        var todo = new Todo
        {
            Title = todoTitle,
            CreatedBy = "alice",
            CreatedAt = now,
            UpdatedAt = now,
            Items = [new TodoItem { Name = itemName, CreatedAt = now, UpdatedAt = now }]
        };
        context.Todos.Add(todo);
        await context.SaveChangesAsync();
        return (todo.Id, todo.Items[0].Id);
    }
}
