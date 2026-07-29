using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portal.Data;
using Portal.Models;
using Portal.ViewModels;

namespace Portal.ViewComponents;

// A shared-layout widget that fetches its own data regardless of which page rendered
// it - the standard MVC tool for "every page needs this, but no single action's model
// should have to carry it." Runs the one query indexed for exactly this shape (see
// PortalContext's Notification index comment): COUNT(*) WHERE RecipientId = me AND
// IsRead = false.
public class NotificationBadgeViewComponent(PortalContext context, UserManager<PortalUser> userManager) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var userId = userManager.GetUserId(UserClaimsPrincipal)!;

        var unreadCount = await context.Notifications
            .CountAsync(notification => notification.RecipientId == userId && !notification.IsRead);

        return View(new NotificationBadgeViewModel { UnreadCount = unreadCount });
    }
}
