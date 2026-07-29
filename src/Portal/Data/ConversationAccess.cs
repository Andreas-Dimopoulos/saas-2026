using Microsoft.EntityFrameworkCore;

namespace Portal.Data;

// The one membership query both ConversationsController and ConversationHub must
// use, unchanged. A hub method is a public endpoint like any action - [Authorize]
// only proves who's calling, not what they may read - so every conversation-scoped
// entry point (HTTP or hub) checks the same thing before doing anything else: does a
// ConversationParticipant row exist for this (conversationId, userId) pair? Sharing
// one implementation means there's only one place this check can be gotten wrong,
// not several copies that can quietly drift apart.
public static class ConversationAccess
{
    public static Task<bool> IsParticipantAsync(this PortalContext context, int conversationId, string userId) =>
        context.ConversationParticipants
            .AnyAsync(participant => participant.ConversationId == conversationId && participant.UserId == userId);
}
