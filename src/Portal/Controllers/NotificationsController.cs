using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portal.Data;
using Portal.Models;
using Portal.ViewModels;

namespace Portal.Controllers;

[Authorize]
public class NotificationsController(PortalContext context, UserManager<PortalUser> userManager) : Controller
{
    public async Task<IActionResult> Index()
    {
        var currentUserId = userManager.GetUserId(User)!;

        var notifications = await context.Notifications
            .Where(notification => notification.RecipientId == currentUserId)
            .OrderByDescending(notification => notification.CreatedAt)
            .Select(notification => new NotificationViewModel
            {
                Id = notification.Id,
                Text = notification.Text,
                CreatedAt = notification.CreatedAt,
                IsRead = notification.IsRead,
                ConversationId = notification.ConversationId
            })
            .ToListAsync();

        return View(notifications);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var currentUserId = userManager.GetUserId(User)!;

        var notification = await context.Notifications.FirstOrDefaultAsync(notification => notification.Id == id);

        if (notification is null || notification.RecipientId != currentUserId)
        {
            return NotFound();
        }

        notification.IsRead = true;
        await context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}
