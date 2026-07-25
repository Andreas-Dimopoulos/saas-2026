using Microsoft.EntityFrameworkCore;
using TodoApi.Models;

namespace TodoApi.Data;

public class TodoContext(DbContextOptions<TodoContext> options) : DbContext(options)
{
    public DbSet<Todo> Todos => Set<Todo>();
    public DbSet<TodoItem> Items => Set<TodoItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Todo>()
            .HasMany(todo => todo.Items)
            .WithOne(item => item.Todo)
            .HasForeignKey(item => item.TodoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
