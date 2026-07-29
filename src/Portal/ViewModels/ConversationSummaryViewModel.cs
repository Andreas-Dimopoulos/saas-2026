namespace Portal.ViewModels;

public class ConversationSummaryViewModel
{
    public required int Id { get; set; }
    public string? Name { get; set; }
    public required IReadOnlyList<string> OtherParticipantNames { get; set; }
    public string? LastMessageBody { get; set; }
    public DateTime? LastMessageAt { get; set; }

    public string DisplayTitle => Name
        ?? (OtherParticipantNames.Count > 0 ? string.Join(", ", OtherParticipantNames) : "(just you)");
}
