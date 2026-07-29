namespace Portal.Models;

public class ConversationParticipant
{
    public int Id { get; set; }

    // Not `required`: participants are usually added via Conversation.Participants
    // (see ConversationsController.Create), before the parent conversation has been
    // saved and assigned an Id - EF's relationship fixup sets this FK on SaveChanges.
    public int ConversationId { get; set; }
    public Conversation Conversation { get; set; } = null!;
    public required string UserId { get; set; }
    public PortalUser User { get; set; } = null!;
    public DateTime JoinedAt { get; set; }
}
