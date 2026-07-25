using System.ComponentModel.DataAnnotations;

namespace TodoApi.Dtos;

public record UpdateTodoRequest([Required(AllowEmptyStrings = false)] string Title);
