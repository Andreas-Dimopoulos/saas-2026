namespace Portal.Models;

public class Message
{
    public int Id { get; set; }
    public required int ConversationId { get; set; }
    public Conversation Conversation { get; set; } = null!;
    public required string SenderId { get; set; }
    public PortalUser Sender { get; set; } = null!;
    public required string Body { get; set; }
    public DateTime SentAt { get; set; }
}
