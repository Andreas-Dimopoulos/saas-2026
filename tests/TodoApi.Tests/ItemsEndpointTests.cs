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
    private const string OwnerEmail = "alice@example.com";
    private const string OtherEmail = "bob@example.com";

    [Fact]
    public async Task GetItem_ReturnsOkWithMatchingItem()
    {
        using var factory = new TodoApiFactory();
        var (todoId, itemId) = await SeedTodoWithItemAsync(factory, "Groceries", "Milk", OwnerEmail);
        var client = await AuthTestHelper.AuthenticatedClientAsync(factory, OwnerEmail);

        var response = await client.GetAsync($"/todos/{todoId}/items/{itemId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(itemId, body.GetProperty("id").GetInt32());
        Assert.Equal("Milk", body.GetProperty("name").GetString());
        Assert.False(body.GetProperty("done").GetBoolean());
    }

    [Fact]
    public async Task GetItem_ReturnsUnauthorized_WithoutToken()
    {
        using var factory = new TodoApiFactory();
        var (todoId, itemId) = await SeedTodoWithItemAsync(factory, "Groceries", "Milk", OwnerEmail);
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/todos/{todoId}/items/{itemId}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetItem_ReturnsNotFound_WhenTodoMissing()
    {
        using var factory = new TodoApiFactory();
        var client = await AuthTestHelper.AuthenticatedClientAsync(factory, OwnerEmail);

        var response = await client.GetAsync("/todos/999/items/1");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetItem_ReturnsNotFound_WhenTodoBelongsToDifferentUser()
    {
        using var factory = new TodoApiFactory();
        var (todoId, itemId) = await SeedTodoWithItemAsync(factory, "Groceries", "Milk", OtherEmail);
        var client = await AuthTestHelper.AuthenticatedClientAsync(factory, OwnerEmail);

        var response = await client.GetAsync($"/todos/{todoId}/items/{itemId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetItem_ReturnsNotFound_WhenItemBelongsToDifferentTodo()
    {
        using var factory = new TodoApiFactory();
        var (_, itemId) = await SeedTodoWithItemAsync(factory, "Groceries", "Milk", OwnerEmail);
        var otherTodoId = await SeedTodoAsync(factory, "Work", OwnerEmail);
        var client = await AuthTestHelper.AuthenticatedClientAsync(factory, OwnerEmail);

        var response = await client.GetAsync($"/todos/{otherTodoId}/items/{itemId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetItem_ReturnsNotFound_WhenItemMissing()
    {
        using var factory = new TodoApiFactory();
        var todoId = await SeedTodoAsync(factory, "Groceries", OwnerEmail);
        var client = await AuthTestHelper.AuthenticatedClientAsync(factory, OwnerEmail);

        var response = await client.GetAsync($"/todos/{todoId}/items/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task PostItem_ReturnsCreatedWithLocationAndBody()
    {
        using var factory = new TodoApiFactory();
        var todoId = await SeedTodoAsync(factory, "Groceries", OwnerEmail);
        var client = await AuthTestHelper.AuthenticatedClientAsync(factory, OwnerEmail);

        var response = await client.PostAsJsonAsync($"/todos/{todoId}/items", new { name = "Milk" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("id").GetInt32() > 0);
        Assert.Equal("Milk", body.GetProperty("name").GetString());
        Assert.False(body.GetProperty("done").GetBoolean());
    }

    [Fact]
    public async Task PostItem_ReturnsNotFound_WhenTodoMissing()
    {
        using var factory = new TodoApiFactory();
        var client = await AuthTestHelper.AuthenticatedClientAsync(factory, OwnerEmail);

        var response = await client.PostAsJsonAsync("/todos/999/items", new { name = "Milk" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task PostItem_ReturnsNotFound_WhenTodoBelongsToDifferentUser()
    {
        using var factory = new TodoApiFactory();
        var todoId = await SeedTodoAsync(factory, "Groceries", OtherEmail);
        var client = await AuthTestHelper.AuthenticatedClientAsync(factory, OwnerEmail);

        var response = await client.PostAsJsonAsync($"/todos/{todoId}/items", new { name = "Milk" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task PostItem_ReturnsBadRequestProblemDetails_WhenNameMissing()
    {
        using var factory = new TodoApiFactory();
        var todoId = await SeedTodoAsync(factory, "Groceries", OwnerEmail);
        var client = await AuthTestHelper.AuthenticatedClientAsync(factory, OwnerEmail);

        var response = await client.PostAsJsonAsync($"/todos/{todoId}/items", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task PutItem_ReturnsOkWithUpdatedItem()
    {
        using var factory = new TodoApiFactory();
        var (todoId, itemId) = await SeedTodoWithItemAsync(factory, "Groceries", "Milk", OwnerEmail);
        var client = await AuthTestHelper.AuthenticatedClientAsync(factory, OwnerEmail);

        var response = await client.PutAsJsonAsync($"/todos/{todoId}/items/{itemId}", new { name = "Oat milk", done = true });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(itemId, body.GetProperty("id").GetInt32());
        Assert.Equal("Oat milk", body.GetProperty("name").GetString());
        Assert.True(body.GetProperty("done").GetBoolean());
    }

    [Fact]
    public async Task PutItem_ReturnsNotFound_WhenTodoMissing()
    {
        using var factory = new TodoApiFactory();
        var (_, itemId) = await SeedTodoWithItemAsync(factory, "Groceries", "Milk", OwnerEmail);
        var client = await AuthTestHelper.AuthenticatedClientAsync(factory, OwnerEmail);

        var response = await client.PutAsJsonAsync($"/todos/999/items/{itemId}", new { name = "Oat milk", done = true });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task PutItem_ReturnsNotFound_WhenTodoBelongsToDifferentUser()
    {
        using var factory = new TodoApiFactory();
        var (todoId, itemId) = await SeedTodoWithItemAsync(factory, "Groceries", "Milk", OtherEmail);
        var client = await AuthTestHelper.AuthenticatedClientAsync(factory, OwnerEmail);

        var response = await client.PutAsJsonAsync($"/todos/{todoId}/items/{itemId}", new { name = "Hijacked", done = true });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task PutItem_ReturnsNotFound_WhenItemBelongsToDifferentTodo()
    {
        using var factory = new TodoApiFactory();
        var (_, itemId) = await SeedTodoWithItemAsync(factory, "Groceries", "Milk", OwnerEmail);
        var otherTodoId = await SeedTodoAsync(factory, "Work", OwnerEmail);
        var client = await AuthTestHelper.AuthenticatedClientAsync(factory, OwnerEmail);

        var response = await client.PutAsJsonAsync($"/todos/{otherTodoId}/items/{itemId}", new { name = "Oat milk", done = true });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task PutItem_ReturnsNotFound_WhenItemMissing()
    {
        using var factory = new TodoApiFactory();
        var todoId = await SeedTodoAsync(factory, "Groceries", OwnerEmail);
        var client = await AuthTestHelper.AuthenticatedClientAsync(factory, OwnerEmail);

        var response = await client.PutAsJsonAsync($"/todos/{todoId}/items/999", new { name = "Oat milk", done = true });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task PutItem_ReturnsBadRequestProblemDetails_WhenNameMissing()
    {
        using var factory = new TodoApiFactory();
        var (todoId, itemId) = await SeedTodoWithItemAsync(factory, "Groceries", "Milk", OwnerEmail);
        var client = await AuthTestHelper.AuthenticatedClientAsync(factory, OwnerEmail);

        var response = await client.PutAsJsonAsync($"/todos/{todoId}/items/{itemId}", new { done = true });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task DeleteItem_ReturnsNoContent()
    {
        using var factory = new TodoApiFactory();
        var (todoId, itemId) = await SeedTodoWithItemAsync(factory, "Groceries", "Milk", OwnerEmail);
        var client = await AuthTestHelper.AuthenticatedClientAsync(factory, OwnerEmail);

        var response = await client.DeleteAsync($"/todos/{todoId}/items/{itemId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteItem_ReturnsNotFound_WhenTodoMissing()
    {
        using var factory = new TodoApiFactory();
        var (_, itemId) = await SeedTodoWithItemAsync(factory, "Groceries", "Milk", OwnerEmail);
        var client = await AuthTestHelper.AuthenticatedClientAsync(factory, OwnerEmail);

        var response = await client.DeleteAsync($"/todos/999/items/{itemId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task DeleteItem_ReturnsNotFound_WhenTodoBelongsToDifferentUser()
    {
        using var factory = new TodoApiFactory();
        var (todoId, itemId) = await SeedTodoWithItemAsync(factory, "Groceries", "Milk", OtherEmail);
        var client = await AuthTestHelper.AuthenticatedClientAsync(factory, OwnerEmail);

        var response = await client.DeleteAsync($"/todos/{todoId}/items/{itemId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task DeleteItem_ReturnsNotFound_WhenItemBelongsToDifferentTodo()
    {
        using var factory = new TodoApiFactory();
        var (_, itemId) = await SeedTodoWithItemAsync(factory, "Groceries", "Milk", OwnerEmail);
        var otherTodoId = await SeedTodoAsync(factory, "Work", OwnerEmail);
        var client = await AuthTestHelper.AuthenticatedClientAsync(factory, OwnerEmail);

        var response = await client.DeleteAsync($"/todos/{otherTodoId}/items/{itemId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task DeleteItem_ReturnsNotFound_WhenItemMissing()
    {
        using var factory = new TodoApiFactory();
        var todoId = await SeedTodoAsync(factory, "Groceries", OwnerEmail);
        var client = await AuthTestHelper.AuthenticatedClientAsync(factory, OwnerEmail);

        var response = await client.DeleteAsync($"/todos/{todoId}/items/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    private static async Task<int> SeedTodoAsync(TodoApiFactory factory, string title, string createdBy)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TodoContext>();
        var now = DateTime.UtcNow;
        var todo = new Todo { Title = title, CreatedBy = createdBy, CreatedAt = now, UpdatedAt = now };
        context.Todos.Add(todo);
        await context.SaveChangesAsync();
        return todo.Id;
    }

    private static async Task<(int TodoId, int ItemId)> SeedTodoWithItemAsync(TodoApiFactory factory, string todoTitle, string itemName, string createdBy)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TodoContext>();
        var now = DateTime.UtcNow;
        var todo = new Todo
        {
            Title = todoTitle,
            CreatedBy = createdBy,
            CreatedAt = now,
            UpdatedAt = now,
            Items = [new TodoItem { Name = itemName, CreatedAt = now, UpdatedAt = now }]
        };
        context.Todos.Add(todo);
        await context.SaveChangesAsync();
        return (todo.Id, todo.Items[0].Id);
    }
}
