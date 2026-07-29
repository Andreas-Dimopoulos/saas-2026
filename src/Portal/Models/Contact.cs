namespace Portal.Models;

public class Contact
{
    public int Id { get; set; }
    public required string OwnerId { get; set; }
    public PortalUser Owner { get; set; } = null!;
    public required string ContactUserId { get; set; }
    public PortalUser ContactUser { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
