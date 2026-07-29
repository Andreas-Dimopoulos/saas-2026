using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portal.Data;
using Portal.Models;
using Portal.ViewModels;

namespace Portal.Controllers;

[Authorize]
public class ConversationsController(PortalContext context, UserManager<PortalUser> userManager) : Controller
{
    public async Task<IActionResult> Index()
    {
        var currentUserId = userManager.GetUserId(User)!;

        // Projected in one query - no N+1 loop fetching each conversation's latest
        // message separately. The anonymous projection first materializes the two
        // correlated subqueries (other participants' names, latest message) as plain
        // columns; ordering and the final view-model shape are applied afterwards so
        // EF only ever has to translate straightforward, well-supported shapes.
        var rows = await context.ConversationParticipants
            .Where(participant => participant.UserId == currentUserId)
            .Select(participant => participant.Conversation)
            .Select(conversation => new
            {
                conversation.Id,
                conversation.Name,
                OtherParticipantNames = conversation.Participants
                    .Where(participant => participant.UserId != currentUserId)
                    .Select(participant => participant.User.DisplayName)
                    .ToList(),
                LastMessageBody = conversation.Messages
                    .OrderByDescending(message => message.SentAt)
                    .Select(message => message.Body)
                    .FirstOrDefault(),
                LastMessageAt = conversation.Messages
                    .OrderByDescending(message => message.SentAt)
                    .Select(message => (DateTime?)message.SentAt)
                    .FirstOrDefault()
            })
            .OrderByDescending(row => row.LastMessageAt)
            .ToListAsync();

        var summaries = rows.Select(row => new ConversationSummaryViewModel
        {
            Id = row.Id,
            Name = row.Name,
            OtherParticipantNames = row.OtherParticipantNames,
            LastMessageBody = row.LastMessageBody,
            LastMessageAt = row.LastMessageAt
        }).ToList();

        return View(summaries);
    }

    public async Task<IActionResult> Show(int id)
    {
        var currentUserId = userManager.GetUserId(User)!;

        // Membership check first, as its own query - a non-participant never causes
        // the conversation's (private) messages to be loaded into memory at all, and
        // gets exactly the same 404 as an id that doesn't exist.
        var isParticipant = await context.ConversationParticipants
            .AnyAsync(participant => participant.ConversationId == id && participant.UserId == currentUserId);

        if (!isParticipant)
        {
            return NotFound();
        }

        var conversation = await context.Conversations
            .Include(conversation => conversation.Participants).ThenInclude(participant => participant.User)
            .Include(conversation => conversation.Messages.OrderBy(message => message.SentAt))
                .ThenInclude(message => message.Sender)
            .FirstAsync(conversation => conversation.Id == id);

        var otherNames = conversation.Participants
            .Where(participant => participant.UserId != currentUserId)
            .Select(participant => participant.User.DisplayName)
            .ToList();

        var messages = conversation.Messages
            .Select(message => new MessageViewModel
            {
                SenderDisplayName = message.Sender.DisplayName,
                Body = message.Body,
                SentAt = message.SentAt,
                IsMine = message.SenderId == currentUserId
            })
            .ToList();

        return View(new ConversationDetailViewModel
        {
            Id = conversation.Id,
            Name = conversation.Name,
            OtherParticipantNames = otherNames,
            Messages = messages
        });
    }

    public async Task<IActionResult> New(string? search)
    {
        var currentUserId = userManager.GetUserId(User)!;

        var query = context.Users.Where(user => user.Id != currentUserId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search}%";
            var normalizedSearch = search.ToUpperInvariant();
            query = query.Where(user =>
                EF.Functions.Like(user.DisplayName, pattern) || user.NormalizedEmail == normalizedSearch);
        }

        var results = await query
            .OrderBy(user => user.DisplayName)
            .Select(user => new UserSearchResultViewModel { Id = user.Id, DisplayName = user.DisplayName })
            .ToListAsync();

        return View(new NewConversationViewModel { Results = results, Search = search });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(List<string> participantIds)
    {
        var currentUserId = userManager.GetUserId(User)!;

        // Never trust the posted list for who the creator is - the creator is always
        // added below, unconditionally, so a conversation without its creator in it
        // isn't a case the code can produce.
        var otherIds = await context.Users
            .Where(user => participantIds.Contains(user.Id) && user.Id != currentUserId)
            .Select(user => user.Id)
            .ToListAsync();

        if (otherIds.Count == 0)
        {
            TempData["ConversationError"] = "Select at least one other person.";
            return RedirectToAction(nameof(New));
        }

        // Reuse an existing 1-to-1 rather than scattering messages across duplicate
        // threads with the same pair. Not schema-enforceable (no unique index over a
        // set of participants), so it's a check-then-act query; not applied to groups
        // (3+ participants), where two threads with identical membership are plausibly
        // separate, intentional conversations rather than an accidental duplicate.
        if (otherIds.Count == 1)
        {
            var otherId = otherIds[0];
            var existingId = await context.Conversations
                .Where(conversation => conversation.Participants.Count == 2)
                .Where(conversation => conversation.Participants.Any(p => p.UserId == currentUserId))
                .Where(conversation => conversation.Participants.Any(p => p.UserId == otherId))
                .Select(conversation => (int?)conversation.Id)
                .FirstOrDefaultAsync();

            if (existingId is not null)
            {
                return RedirectToAction(nameof(Show), new { id = existingId });
            }
        }

        var now = DateTime.UtcNow;
        var newConversation = new Conversation { CreatedAt = now };

        foreach (var userId in otherIds.Append(currentUserId))
        {
            newConversation.Participants.Add(new ConversationParticipant { UserId = userId, JoinedAt = now });
        }

        context.Conversations.Add(newConversation);
        await context.SaveChangesAsync();

        return RedirectToAction(nameof(Show), new { id = newConversation.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendMessage(int id, SendMessageViewModel model)
    {
        var currentUserId = userManager.GetUserId(User)!;

        var isParticipant = await context.ConversationParticipants
            .AnyAsync(participant => participant.ConversationId == id && participant.UserId == currentUserId);

        if (!isParticipant)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return RedirectToAction(nameof(Show), new { id });
        }

        context.Messages.Add(new Message
        {
            ConversationId = id,
            SenderId = currentUserId,
            Body = model.Body,
            SentAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        return RedirectToAction(nameof(Show), new { id });
    }
}
