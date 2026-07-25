using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.Dtos;
using TodoApi.Models;

namespace TodoApi.Controllers;

[ApiController]
[Route("todos/{todoId:int}/items")]
public class ItemsController(TodoContext context) : ControllerBase
{
    private static readonly Expression<Func<TodoItem, TodoItemResponse>> ToResponse = item =>
        new TodoItemResponse(item.Id, item.Name, item.Done, item.CreatedAt, item.UpdatedAt);

    [HttpGet("{itemId:int}")]
    public async Task<ActionResult<TodoItemResponse>> GetItem(int todoId, int itemId)
    {
        if (!await context.Todos.AnyAsync(t => t.Id == todoId))
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
}
