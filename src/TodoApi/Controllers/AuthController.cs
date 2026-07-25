using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.Dtos;
using TodoApi.Models;
using TodoApi.Services;

namespace TodoApi.Controllers;

[ApiController]
[Route("auth")]
public class AuthController(TodoContext context, IPasswordHasher<User> passwordHasher, TokenService tokenService) : ControllerBase
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

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user is null)
        {
            return Unauthorized();
        }

        var verification = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            return Unauthorized();
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
            await context.SaveChangesAsync();
        }

        var (token, expiresAt) = tokenService.CreateToken(user);
        return Ok(new LoginResponse(token, expiresAt));
    }

    [Authorize]
    [HttpGet("logout")]
    public async Task<IActionResult> Logout()
    {
        var jti = User.FindFirstValue(JwtRegisteredClaimNames.Jti);
        var expClaim = User.FindFirstValue(JwtRegisteredClaimNames.Exp);
        var expiresAt = DateTimeOffset.FromUnixTimeSeconds(long.Parse(expClaim!)).UtcDateTime;

        var expired = context.RevokedTokens.Where(revoked => revoked.ExpiresAt <= DateTime.UtcNow);
        context.RevokedTokens.RemoveRange(expired);

        context.RevokedTokens.Add(new RevokedToken { Jti = jti!, ExpiresAt = expiresAt });
        await context.SaveChangesAsync();

        return NoContent();
    }
}
