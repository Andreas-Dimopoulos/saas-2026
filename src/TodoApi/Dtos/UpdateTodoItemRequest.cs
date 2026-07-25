using System.ComponentModel.DataAnnotations;

namespace TodoApi.Dtos;

public record UpdateTodoItemRequest([Required(AllowEmptyStrings = false)] string Name, bool Done);
