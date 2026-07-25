namespace TodoApi.Dtos;

public record TodoItemResponse(int Id, string Name, bool Done, DateTime CreatedAt, DateTime UpdatedAt);
