namespace TodoApi.Dtos;

public record TodoResponse(
    int Id,
    string Title,
    string CreatedBy,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<TodoItemResponse> Items);
