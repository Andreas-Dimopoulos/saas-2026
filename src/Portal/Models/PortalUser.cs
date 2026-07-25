using Microsoft.AspNetCore.Identity;

namespace Portal.Models;

public class PortalUser : IdentityUser
{
    public required string DisplayName { get; set; }
}
