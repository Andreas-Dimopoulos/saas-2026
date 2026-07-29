namespace Portal.ViewModels;

public class MessageViewModel
{
    public required string SenderDisplayName { get; set; }
    public required string Body { get; set; }
    public required DateTime SentAt { get; set; }
    public required bool IsMine { get; set; }
}
