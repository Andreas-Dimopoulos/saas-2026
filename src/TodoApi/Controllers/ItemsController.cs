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
/// CRUD operations on the items of a todo owned by the authenticated user. A todo
/// that exists but belongs to a different user is indistinguishable, from the
/// response, from a todo that doesn't exist at all.
/// </summary>
[ApiController]
[Authorize]
[Route("todos/{todoId:int}/items")]
public class ItemsController(TodoContext context) : ControllerBase
{
    private static readonly Expression<Func<TodoItem, TodoItemResponse>> ToResponse = item =>
        new TodoItemResponse(item.Id, item.Name, item.Done, item.CreatedAt, item.UpdatedAt);

    /// <summary>
    /// Fetches a single item belonging to a todo owned by the caller.
    /// </summary>
    /// <param name="todoId">The parent todo's id.</param>
    /// <param name="itemId">The item's id.</param>
    /// <response code="200">The matching item.</response>
    /// <response code="401">No valid token was supplied.</response>
    /// <response code="404">The todo doesn't exist (or isn't the caller's), or the item doesn't belong to that todo (application/problem+json).</response>
    [HttpGet("{itemId:int}")]
    [ProducesResponseType(typeof(TodoItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
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

    /// <summary>
    /// Adds a new item to a todo owned by the caller.
    /// </summary>
    /// <param name="todoId">The parent todo's id.</param>
    /// <param name="request">The item's name.</param>
    /// <returns>The newly created item.</returns>
    /// <response code="201">The item was created.</response>
    /// <response code="400">Name was missing or empty (application/problem+json).</response>
    /// <response code="401">No valid token was supplied.</response>
    /// <response code="404">The todo doesn't exist, or isn't the caller's (application/problem+json).</response>
    [HttpPost]
    [ProducesResponseType(typeof(TodoItemResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
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

    /// <summary>
    /// Replaces the name and done status of an item belonging to a todo owned by the caller.
    /// </summary>
    /// <param name="todoId">The parent todo's id.</param>
    /// <param name="itemId">The item's id.</param>
    /// <param name="request">The item's new name and done status.</param>
    /// <returns>The updated item.</returns>
    /// <response code="200">The updated item.</response>
    /// <response code="400">Name was missing or empty (application/problem+json).</response>
    /// <response code="401">No valid token was supplied.</response>
    /// <response code="404">The todo doesn't exist (or isn't the caller's), or the item doesn't belong to that todo (application/problem+json).</response>
    [HttpPut("{itemId:int}")]
    [ProducesResponseType(typeof(TodoItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
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

    /// <summary>
    /// Deletes an item belonging to a todo owned by the caller.
    /// </summary>
    /// <param name="todoId">The parent todo's id.</param>
    /// <param name="itemId">The item's id.</param>
    /// <response code="204">The item was deleted.</response>
    /// <response code="401">No valid token was supplied.</response>
    /// <response code="404">The todo doesn't exist (or isn't the caller's), or the item doesn't belong to that todo (application/problem+json).</response>
    [HttpDelete("{itemId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
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
