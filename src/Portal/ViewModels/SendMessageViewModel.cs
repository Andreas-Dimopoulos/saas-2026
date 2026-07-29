using System.ComponentModel.DataAnnotations;

namespace Portal.ViewModels;

public class SendMessageViewModel
{
    [Required]
    public string Body { get; set; } = string.Empty;
}
