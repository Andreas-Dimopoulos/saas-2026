using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Portal.Models;

namespace Portal.Data;

public class PortalContext(DbContextOptions<PortalContext> options) : IdentityDbContext<PortalUser>(options)
{
}
