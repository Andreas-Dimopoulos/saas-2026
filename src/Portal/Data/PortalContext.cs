using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Portal.Models;

namespace Portal.Data;

public class PortalContext(DbContextOptions<PortalContext> options) : IdentityDbContext<PortalUser>(options)
{
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<Contact> Contacts => Set<Contact>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Post>()
            .HasOne(post => post.Author)
            .WithMany()
            .HasForeignKey(post => post.AuthorId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Post>()
            .Property(post => post.Category)
            .HasConversion<string>();

        modelBuilder.Entity<Contact>()
            .HasOne(contact => contact.Owner)
            .WithMany()
            .HasForeignKey(contact => contact.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict, not Cascade: deleting a user who appears in someone else's contact
        // list should fail loudly rather than silently mutating another user's owned
        // data as a side effect. There's no user-deletion feature yet, so this is
        // untested in practice - revisit if one is ever added.
        modelBuilder.Entity<Contact>()
            .HasOne(contact => contact.ContactUser)
            .WithMany()
            .HasForeignKey(contact => contact.ContactUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Contact>()
            .HasIndex(contact => new { contact.OwnerId, contact.ContactUserId })
            .IsUnique();

        modelBuilder.Entity<Contact>().ToTable(table =>
            table.HasCheckConstraint("CK_Contact_NoSelfContact", "\"OwnerId\" <> \"ContactUserId\""));
    }
}
