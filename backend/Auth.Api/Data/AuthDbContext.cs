using Example.Com.InterviewQuestion002.Entities;
using Microsoft.EntityFrameworkCore;

namespace Example.Com.InterviewQuestion002.Data;

public sealed class AuthDbContext(DbContextOptions<AuthDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var user = modelBuilder.Entity<User>();
        user.ToTable("Users");
        user.HasKey(static entity => entity.Id);
        user.Property(static entity => entity.Username).HasMaxLength(256).IsRequired();
        user.Property(static entity => entity.NormalizedUsername).HasMaxLength(256).IsRequired();
        user.HasIndex(static entity => entity.NormalizedUsername).IsUnique();
        user.Property(static entity => entity.PasswordHash).HasMaxLength(1024).IsRequired();
        user.Property(static entity => entity.CreatedAtUtc).IsRequired();
    }
}
