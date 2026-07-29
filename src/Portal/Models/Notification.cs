namespace Portal.Models;

public class Notification
{
    public int Id { get; set; }
    public required string RecipientId { get; set; }
    public PortalUser Recipient { get; set; } = null!;

    // The actor's id, kept alongside the precomputed Text below - lets a notification
    // link back to who triggered it, and leaves future re-rendering possible without
    // a migration. Nullable and SetNull on delete (not Restrict, not Cascade): if the
    // actor is ever deleted, the notification - and its frozen Text - must survive;
    // only the live reference is severed. Deliberate consequence: Text is a snapshot
    // taken at creation time, not re-derived from the actor's current DisplayName, so
    // a later display-name change does not retroactively change historical
    // notifications - they show the name as it was when the event happened.
    public string? ActorId { get; set; }
    public PortalUser? Actor { get; set; }

    public required NotificationKind Kind { get; set; }
    public required string Text { get; set; }

    public int? ConversationId { get; set; }
    public Conversation? Conversation { get; set; }

    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }
}
