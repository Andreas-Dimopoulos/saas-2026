namespace Portal.ViewModels;

public class ConversationDetailViewModel
{
    public required int Id { get; set; }
    public string? Name { get; set; }
    public required IReadOnlyList<string> OtherParticipantNames { get; set; }
    public required IReadOnlyList<MessageViewModel> Messages { get; set; }

    public string DisplayTitle => Name
        ?? (OtherParticipantNames.Count > 0 ? string.Join(", ", OtherParticipantNames) : "(just you)");
}
