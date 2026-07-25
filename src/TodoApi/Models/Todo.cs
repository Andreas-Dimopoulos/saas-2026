namespace TodoApi.Models;

public class Todo
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public required string CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public List<TodoItem> Items { get; set; } = [];
}
