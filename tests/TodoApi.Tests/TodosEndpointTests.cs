using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TodoApi.Data;
using TodoApi.Models;

namespace TodoApi.Tests;

public class TodosEndpointTests
{
    [Fact]
    public async Task GetTodos_ReturnsOkWithEmptyArray_WhenNoneExist()
    {
        using var factory = new TodoApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/todos");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var todos = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Array, todos.ValueKind);
        Assert.Empty(todos.EnumerateArray());
    }

    [Fact]
    public async Task PostTodos_ReturnsCreatedWithLocationAndBody()
    {
        using var factory = new TodoApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/todos", new { title = "Buy milk", createdBy = "alice" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("id").GetInt32() > 0);
        Assert.Equal("Buy milk", body.GetProperty("title").GetString());
        Assert.Equal("alice", body.GetProperty("createdBy").GetString());
        Assert.Equal(JsonValueKind.Array, body.GetProperty("items").ValueKind);
        Assert.Empty(body.GetProperty("items").EnumerateArray());
    }

    [Fact]
    public async Task GetTodoById_ReturnsOkWithMatchingTodo()
    {
        using var factory = new TodoApiFactory();
        var todoId = await SeedTodoAsync(factory, "Groceries", "alice");
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/todos/{todoId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(todoId, body.GetProperty("id").GetInt32());
        Assert.Equal("Groceries", body.GetProperty("title").GetString());
        Assert.Equal("alice", body.GetProperty("createdBy").GetString());
    }

    [Fact]
    public async Task GetTodoById_ReturnsNotFound_WhenMissing()
    {
        using var factory = new TodoApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/todos/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task PutTodoById_ReturnsOkWithUpdatedTodo()
    {
        using var factory = new TodoApiFactory();
        var todoId = await SeedTodoAsync(factory, "Groceries", "alice");
        var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync($"/todos/{todoId}", new { title = "Groceries v2", createdBy = "alice" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(todoId, body.GetProperty("id").GetInt32());
        Assert.Equal("Groceries v2", body.GetProperty("title").GetString());
    }

    [Fact]
    public async Task DeleteTodoById_ReturnsNoContent()
    {
        using var factory = new TodoApiFactory();
        var todoId = await SeedTodoAsync(factory, "Groceries", "alice");
        var client = factory.CreateClient();

        var response = await client.DeleteAsync($"/todos/{todoId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteTodoById_AlsoDeletesItems()
    {
        using var factory = new TodoApiFactory();
        int todoId;
        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<TodoContext>();
            var now = DateTime.UtcNow;
            var todo = new Todo
            {
                Title = "Groceries",
                CreatedBy = "alice",
                CreatedAt = now,
                UpdatedAt = now,
                Items =
                [
                    new TodoItem { Name = "Milk", CreatedAt = now, UpdatedAt = now },
                    new TodoItem { Name = "Eggs", CreatedAt = now, UpdatedAt = now }
                ]
            };
            context.Todos.Add(todo);
            await context.SaveChangesAsync();
            todoId = todo.Id;
        }

        var client = factory.CreateClient();
        var response = await client.DeleteAsync($"/todos/{todoId}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<TodoContext>();
            Assert.Null(await context.Todos.FindAsync(todoId));
            Assert.Empty(await context.Items.Where(item => item.TodoId == todoId).ToListAsync());
        }
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
}
