using System.ComponentModel.DataAnnotations;
using Portal.Models;

namespace Portal.ViewModels;

public class PostFormViewModel
{
    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Body { get; set; } = string.Empty;

    public PostCategory Category { get; set; }
}
