namespace TodoApi.Models;

public class TodoItem
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public bool Done { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public int TodoId { get; set; }
    public Todo? Todo { get; set; }
}
