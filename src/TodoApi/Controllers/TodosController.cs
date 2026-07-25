using System.Linq.Expressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.Dtos;
using TodoApi.Extensions;
using TodoApi.Models;

namespace TodoApi.Controllers;

/// <summary>
/// CRUD operations on the authenticated user's own todos. Every action requires a
/// bearer token and only ever sees todos owned by that token's account.
/// </summary>
[ApiController]
[Authorize]
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

    /// <summary>
    /// Lists the caller's own todos.
    /// </summary>
    /// <returns>The caller's todos, each with its items.</returns>
    /// <response code="200">The list of todos, possibly empty.</response>
    /// <response code="401">No valid token was supplied.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TodoResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<TodoResponse>>> GetTodos()
    {
        var email = User.GetEmail();
        var todos = await context.Todos.Where(t => t.CreatedBy == email).Select(ToResponse).ToListAsync();
        return Ok(todos);
    }

    /// <summary>
    /// Fetches a single todo owned by the caller.
    /// </summary>
    /// <param name="id">The todo's id.</param>
    /// <response code="200">The matching todo.</response>
    /// <response code="401">No valid token was supplied.</response>
    /// <response code="404">No such todo, or it belongs to a different user (application/problem+json).</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(TodoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TodoResponse>> GetTodo(int id)
    {
        var email = User.GetEmail();
        var todo = await context.Todos
            .Where(t => t.Id == id && t.CreatedBy == email)
            .Select(ToResponse)
            .FirstOrDefaultAsync();

        if (todo is null)
        {
            return NotFound();
        }

        return Ok(todo);
    }

    /// <summary>
    /// Creates a new todo owned by the caller.
    /// </summary>
    /// <param name="request">The todo's title.</param>
    /// <returns>The newly created todo.</returns>
    /// <response code="201">The todo was created.</response>
    /// <response code="400">Title was missing or empty (application/problem+json).</response>
    /// <response code="401">No valid token was supplied.</response>
    [HttpPost]
    [ProducesResponseType(typeof(TodoResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TodoResponse>> CreateTodo(CreateTodoRequest request)
    {
        var email = User.GetEmail();
        var now = DateTime.UtcNow;
        var todo = new Todo
        {
            Title = request.Title,
            CreatedBy = email,
            CreatedAt = now,
            UpdatedAt = now
        };

        context.Todos.Add(todo);
        await context.SaveChangesAsync();

        var response = new TodoResponse(todo.Id, todo.Title, todo.CreatedBy, todo.CreatedAt, todo.UpdatedAt, []);
        return CreatedAtAction(nameof(GetTodo), new { id = todo.Id }, response);
    }

    /// <summary>
    /// Replaces the title of a todo owned by the caller.
    /// </summary>
    /// <param name="id">The todo's id.</param>
    /// <param name="request">The todo's new title.</param>
    /// <returns>The updated todo.</returns>
    /// <response code="200">The updated todo.</response>
    /// <response code="400">Title was missing or empty (application/problem+json).</response>
    /// <response code="401">No valid token was supplied.</response>
    /// <response code="404">No such todo, or it belongs to a different user (application/problem+json).</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(TodoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TodoResponse>> UpdateTodo(int id, UpdateTodoRequest request)
    {
        var email = User.GetEmail();
        var todo = await context.Todos.FirstOrDefaultAsync(t => t.Id == id && t.CreatedBy == email);

        if (todo is null)
        {
            return NotFound();
        }

        todo.Title = request.Title;
        todo.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        var response = await context.Todos.Where(t => t.Id == id).Select(ToResponse).FirstAsync();
        return Ok(response);
    }

    /// <summary>
    /// Deletes a todo owned by the caller, along with all of its items (cascade delete).
    /// </summary>
    /// <param name="id">The todo's id.</param>
    /// <response code="204">The todo (and its items) were deleted.</response>
    /// <response code="401">No valid token was supplied.</response>
    /// <response code="404">No such todo, or it belongs to a different user (application/problem+json).</response>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTodo(int id)
    {
        var email = User.GetEmail();
        var todo = await context.Todos.FirstOrDefaultAsync(t => t.Id == id && t.CreatedBy == email);

        if (todo is null)
        {
            return NotFound();
        }

        context.Todos.Remove(todo);
        await context.SaveChangesAsync();

        return NoContent();
    }
}
