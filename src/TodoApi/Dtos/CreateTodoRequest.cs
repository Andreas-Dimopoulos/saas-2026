using System.ComponentModel.DataAnnotations;

namespace TodoApi.Dtos;

public record CreateTodoRequest([Required(AllowEmptyStrings = false)] string Title);
