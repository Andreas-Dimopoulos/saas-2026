using Portal.Models;

namespace Portal.ViewModels;

public class PostsIndexViewModel
{
    public required IReadOnlyList<Post> Posts { get; init; }
    public string? Search { get; init; }
    public PostCategory? Category { get; init; }
}
