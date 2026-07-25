using System.ComponentModel.DataAnnotations;

namespace TodoApi.Dtos;

public record CreateTodoItemRequest([Required(AllowEmptyStrings = false)] string Name);
