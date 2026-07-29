using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Portal.Models;

namespace Portal.Data;

public class PortalContext(DbContextOptions<PortalContext> options) : IdentityDbContext<PortalUser>(options)
{
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ConversationParticipant> ConversationParticipants => Set<ConversationParticipant>();
    public DbSet<Message> Messages => Set<Message>();

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

        // A participant row can't outlive its conversation, and a message can't
        // outlive the conversation it was posted in - both cascade.
        modelBuilder.Entity<ConversationParticipant>()
            .HasOne(participant => participant.Conversation)
            .WithMany(conversation => conversation.Participants)
            .HasForeignKey(participant => participant.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Message>()
            .HasOne(message => message.Conversation)
            .WithMany(conversation => conversation.Messages)
            .HasForeignKey(message => message.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict, not Cascade, for both user-facing FKs below - same reasoning as
        // Contact.ContactUserId above. There's no user-deletion feature yet; when one
        // exists, deleting a user who's a participant somewhere - or who sent messages
        // still visible to other, undeleted participants - should fail loudly rather
        // than silently removing them from someone else's conversation (Participant)
        // or erasing someone else's message history out from under them (Message).
        // Untested in practice for the same reason as Contact - revisit if user
        // deletion is ever added.
        modelBuilder.Entity<ConversationParticipant>()
            .HasOne(participant => participant.User)
            .WithMany()
            .HasForeignKey(participant => participant.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Message>()
            .HasOne(message => message.Sender)
            .WithMany()
            .HasForeignKey(message => message.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        // The guarantee: a user can't be added to the same conversation twice. This
        // closes the same concurrent-add race the Contact unique index closes above.
        // There's no equivalent schema constraint for "two conversations with the same
        // participant set" (see ConversationsController.Create) - that's a set-based
        // condition across rows with no natural unique-index shape, and unlike this
        // one, it's not guarding a security-relevant invariant, just a UX convenience
        // for 1-to-1s - so it's handled with a check-then-act query instead.
        modelBuilder.Entity<ConversationParticipant>()
            .HasIndex(participant => new { participant.ConversationId, participant.UserId })
            .IsUnique();
    }
}
