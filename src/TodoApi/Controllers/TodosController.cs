using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.Dtos;
using TodoApi.Models;

namespace TodoApi.Controllers;

[ApiController]
[Route("todos")]
public class TodosController(TodoContext context) : ControllerBase
{
    private static readonly Expression<Func<Todo, TodoResponse>> ToResponse = todo => new TodoResponse(
        todo.Id,
        todo.Title,
        todo.CreatedBy,
        todo.CreatedAt,
        todo.UpdatedAt,
        todo.Items.Select(item => new TodoItemResponse(item.Id, item.Name, item.Done, item.CreatedAt, item.UpdatedAt)).ToList());

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TodoResponse>>> GetTodos()
    {
        var todos = await context.Todos.Select(ToResponse).ToListAsync();
        return Ok(todos);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TodoResponse>> GetTodo(int id)
    {
        var todo = await context.Todos.Where(t => t.Id == id).Select(ToResponse).FirstOrDefaultAsync();

        if (todo is null)
        {
            return NotFound();
        }

        return Ok(todo);
    }

    [HttpPost]
    public async Task<ActionResult<TodoResponse>> CreateTodo(CreateTodoRequest request)
    {
        var now = DateTime.UtcNow;
        var todo = new Todo
        {
            Title = request.Title,
            CreatedBy = request.CreatedBy,
            CreatedAt = now,
            UpdatedAt = now
        };

        context.Todos.Add(todo);
        await context.SaveChangesAsync();

        var response = new TodoResponse(todo.Id, todo.Title, todo.CreatedBy, todo.CreatedAt, todo.UpdatedAt, []);
        return CreatedAtAction(nameof(GetTodo), new { id = todo.Id }, response);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<TodoResponse>> UpdateTodo(int id, UpdateTodoRequest request)
    {
        var todo = await context.Todos.FindAsync(id);

        if (todo is null)
        {
            return NotFound();
        }

        todo.Title = request.Title;
        todo.CreatedBy = request.CreatedBy;
        todo.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        var response = await context.Todos.Where(t => t.Id == id).Select(ToResponse).FirstAsync();
        return Ok(response);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteTodo(int id)
    {
        var todo = await context.Todos.FindAsync(id);

        if (todo is null)
        {
            return NotFound();
        }

        context.Todos.Remove(todo);
        await context.SaveChangesAsync();

        return NoContent();
    }
}
