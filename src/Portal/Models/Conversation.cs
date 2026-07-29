namespace Portal.Models;

public class Conversation
{
    public int Id { get; set; }

    // Null for a DM or an unnamed group - display falls back to the other
    // participants' names in that case (see ConversationSummaryViewModel.DisplayTitle).
    public string? Name { get; set; }

    public DateTime CreatedAt { get; set; }

    public ICollection<ConversationParticipant> Participants { get; set; } = [];
    public ICollection<Message> Messages { get; set; } = [];
}
