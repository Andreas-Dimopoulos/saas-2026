using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Portal.Controllers;

// Spike only - throwaway page to drive the ChatHub connectivity spike. Not part of
// the real direct-messaging feature.
[Authorize]
public class ChatController : Controller
{
    public IActionResult Index() => View();
}
