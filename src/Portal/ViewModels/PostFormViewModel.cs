using System.ComponentModel.DataAnnotations;

namespace Portal.ViewModels;

public class PostFormViewModel
{
    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Body { get; set; } = string.Empty;

    [Required]
    public string Category { get; set; } = string.Empty;
}
