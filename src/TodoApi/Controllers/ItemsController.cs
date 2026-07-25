using System.Linq.Expressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.Dtos;
using TodoApi.Extensions;
using TodoApi.Models;

namespace TodoApi.Controllers;

[ApiController]
[Authorize]
[Route("todos/{todoId:int}/items")]
public class ItemsController(TodoContext context) : ControllerBase
{
    private static readonly Expression<Func<TodoItem, TodoItemResponse>> ToResponse = item =>
        new TodoItemResponse(item.Id, item.Name, item.Done, item.CreatedAt, item.UpdatedAt);

    [HttpGet("{itemId:int}")]
    public async Task<ActionResult<TodoItemResponse>> GetItem(int todoId, int itemId)
    {
        var email = User.GetEmail();
        if (!await context.Todos.AnyAsync(t => t.Id == todoId && t.CreatedBy == email))
        {
            return NotFound();
        }

        var item = await context.Items
            .Where(i => i.Id == itemId && i.TodoId == todoId)
            .Select(ToResponse)
            .FirstOrDefaultAsync();

        if (item is null)
        {
            return NotFound();
        }

        return Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<TodoItemResponse>> CreateItem(int todoId, CreateTodoItemRequest request)
    {
        var email = User.GetEmail();
        if (!await context.Todos.AnyAsync(t => t.Id == todoId && t.CreatedBy == email))
        {
            return NotFound();
        }

        var now = DateTime.UtcNow;
        var item = new TodoItem
        {
            Name = request.Name,
            Done = false,
            TodoId = todoId,
            CreatedAt = now,
            UpdatedAt = now
        };

        context.Items.Add(item);
        await context.SaveChangesAsync();

        var response = new TodoItemResponse(item.Id, item.Name, item.Done, item.CreatedAt, item.UpdatedAt);
        return CreatedAtAction(nameof(GetItem), new { todoId, itemId = item.Id }, response);
    }

    [HttpPut("{itemId:int}")]
    public async Task<ActionResult<TodoItemResponse>> UpdateItem(int todoId, int itemId, UpdateTodoItemRequest request)
    {
        var email = User.GetEmail();
        if (!await context.Todos.AnyAsync(t => t.Id == todoId && t.CreatedBy == email))
        {
            return NotFound();
        }

        var item = await context.Items.FirstOrDefaultAsync(i => i.Id == itemId && i.TodoId == todoId);

        if (item is null)
        {
            return NotFound();
        }

        item.Name = request.Name;
        item.Done = request.Done;
        item.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        return Ok(new TodoItemResponse(item.Id, item.Name, item.Done, item.CreatedAt, item.UpdatedAt));
    }

    [HttpDelete("{itemId:int}")]
    public async Task<IActionResult> DeleteItem(int todoId, int itemId)
    {
        var email = User.GetEmail();
        if (!await context.Todos.AnyAsync(t => t.Id == todoId && t.CreatedBy == email))
        {
            return NotFound();
        }

        var item = await context.Items.FirstOrDefaultAsync(i => i.Id == itemId && i.TodoId == todoId);

        if (item is null)
        {
            return NotFound();
        }

        context.Items.Remove(item);
        await context.SaveChangesAsync();

        return NoContent();
    }
}
