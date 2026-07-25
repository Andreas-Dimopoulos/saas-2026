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
    /// <summary>
    /// Registers a new user account.
    /// </summary>
    /// <param name="request">The email and password to register.</param>
    /// <returns>The newly created account.</returns>
    /// <response code="201">The account was created.</response>
    /// <response code="400">The email or password was missing or the password failed the complexity check (application/problem+json).</response>
    /// <response code="409">An account with this email is already registered (application/problem+json).</response>
    [HttpPost("/signup")]
    [ProducesResponseType(typeof(SignupResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
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

    /// <summary>
    /// Exchanges valid credentials for a JWT.
    /// </summary>
    /// <param name="request">The account's email and password.</param>
    /// <returns>A bearer token and its expiry.</returns>
    /// <response code="200">The token was issued.</response>
    /// <response code="401">The email is not registered, or the password is wrong.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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

    /// <summary>
    /// Revokes the caller's current token. The token's jti is denylisted, so it is
    /// rejected by every subsequent request even though it hasn't expired yet.
    /// </summary>
    /// <response code="204">The token was revoked.</response>
    /// <response code="401">No token was supplied, or it's already invalid (expired, revoked, or malformed).</response>
    [Authorize]
    [HttpGet("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
