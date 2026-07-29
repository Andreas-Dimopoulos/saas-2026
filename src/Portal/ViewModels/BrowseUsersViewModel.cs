namespace Portal.ViewModels;

public class BrowseUsersViewModel
{
    public required IReadOnlyList<UserSearchResultViewModel> Results { get; init; }
    public string? Search { get; init; }
}
