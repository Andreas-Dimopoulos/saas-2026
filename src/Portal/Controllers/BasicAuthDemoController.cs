using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portal.Authentication;
using Portal.Dtos;

namespace Portal.Controllers;

[ApiController]
[Route("api")]
public class BasicAuthDemoController : ControllerBase
{
    [HttpGet("me")]
    [Authorize(AuthenticationSchemes = BasicAuthenticationDefaults.AuthenticationScheme)]
    public IActionResult Me()
    {
        var email = User.FindFirstValue(ClaimTypes.Name)!;
        var displayName = User.FindFirstValue("DisplayName")!;
        return Ok(new MeResponse(email, displayName));
    }
}
