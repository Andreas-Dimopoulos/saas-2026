namespace Portal.Models;

public class Post
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public required string Body { get; set; }
    public required PostCategory Category { get; set; }
    public required string AuthorId { get; set; }
    public PortalUser Author { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
