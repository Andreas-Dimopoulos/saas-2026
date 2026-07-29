namespace Portal.ViewModels;

public class NotificationViewModel
{
    public required int Id { get; set; }
    public required string Text { get; set; }
    public required DateTime CreatedAt { get; set; }
    public required bool IsRead { get; set; }
    public int? ConversationId { get; set; }
}
