namespace Portal.ViewModels;

public class NewConversationViewModel
{
    public required IReadOnlyList<UserSearchResultViewModel> Results { get; init; }
    public string? Search { get; init; }
}
