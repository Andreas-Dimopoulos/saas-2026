using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portal.Data;
using Portal.Models;
using Portal.ViewModels;

namespace Portal.Controllers;

[Authorize]
public class ContactsController(PortalContext context, UserManager<PortalUser> userManager) : Controller
{
    public async Task<IActionResult> Index()
    {
        var currentUserId = userManager.GetUserId(User)!;

        var contacts = await context.Contacts
            .Include(contact => contact.ContactUser)
            .Where(contact => contact.OwnerId == currentUserId)
            .OrderBy(contact => contact.ContactUser.DisplayName)
            .ToListAsync();

        return View(contacts);
    }

    public async Task<IActionResult> Browse(string? search)
    {
        var currentUserId = userManager.GetUserId(User)!;

        var existingContactIds = context.Contacts
            .Where(contact => contact.OwnerId == currentUserId)
            .Select(contact => contact.ContactUserId);

        var query = context.Users
            .Where(user => user.Id != currentUserId)
            .Where(user => !existingContactIds.Contains(user.Id));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search}%";
            var normalizedSearch = search.ToUpperInvariant();
            // DisplayName is a partial, case-insensitive match; email is exact only (and
            // compared against NormalizedEmail, not Email, so case doesn't matter). Partial
            // email matching would turn this into an enumerable directory - exact only gives
            // findability to someone who already knows the address, nothing more.
            query = query.Where(user =>
                EF.Functions.Like(user.DisplayName, pattern) || user.NormalizedEmail == normalizedSearch);
        }

        var results = await query
            .OrderBy(user => user.DisplayName)
            .Select(user => new UserSearchResultViewModel { Id = user.Id, DisplayName = user.DisplayName })
            .ToListAsync();

        return View(new BrowseUsersViewModel { Results = results, Search = search });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(string contactUserId)
    {
        var currentUserId = userManager.GetUserId(User)!;

        // App-level checks: for the message. The database CHECK constraint and unique
        // index below are for the truth - they're what actually closes the race between
        // two concurrent adds of the same pair. These checks just give the common,
        // non-race case a readable response instead of a raw constraint-violation 500.
        if (contactUserId == currentUserId)
        {
            TempData["ContactError"] = "You can't add yourself as a contact.";
            return RedirectToAction(nameof(Browse));
        }

        var alreadyExists = await context.Contacts
            .AnyAsync(contact => contact.OwnerId == currentUserId && contact.ContactUserId == contactUserId);
        if (alreadyExists)
        {
            TempData["ContactError"] = "This person is already in your contacts.";
            return RedirectToAction(nameof(Browse));
        }

        context.Contacts.Add(new Contact
        {
            OwnerId = currentUserId,
            ContactUserId = contactUserId,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int id)
    {
        var currentUserId = userManager.GetUserId(User)!;
        var contact = await context.Contacts.FirstOrDefaultAsync(contact => contact.Id == id);

        if (contact is null || contact.OwnerId != currentUserId)
        {
            return NotFound();
        }

        context.Contacts.Remove(contact);
        await context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}
