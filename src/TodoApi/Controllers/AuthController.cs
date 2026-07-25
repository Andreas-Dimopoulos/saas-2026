using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.Dtos;
using TodoApi.Models;

namespace TodoApi.Controllers;

[ApiController]
[Route("auth")]
public class AuthController(TodoContext context, IPasswordHasher<User> passwordHasher) : ControllerBase
{
    [HttpPost("/signup")]
    public async Task<ActionResult<SignupResponse>> SignUp(SignupRequest request)
    {
        if (await context.Users.AnyAsync(u => u.Email == request.Email))
        {
            return Problem(title: "Email already registered", statusCode: StatusCodes.Status409Conflict);
        }

        var user = new User
        {
            Email = request.Email,
            PasswordHash = string.Empty,
            CreatedAt = DateTime.UtcNow
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var response = new SignupResponse(user.Id, user.Email, user.CreatedAt);
        return StatusCode(StatusCodes.Status201Created, response);
    }
}
