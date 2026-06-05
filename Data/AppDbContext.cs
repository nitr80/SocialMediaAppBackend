using Microsoft.EntityFrameworkCore;
using SocialMediaAppBackend.Models;

public class AppDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Post> Posts { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<Like> Likes { get; set; }
    public DbSet<CommentLike> CommentLikes { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {

    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
        modelBuilder.Entity<User>().HasIndex(u => u.Username).IsUnique();

        modelBuilder.Entity<Like>().HasIndex(l => new {l.PostId, l.UserId}).IsUnique();

        modelBuilder.Entity<CommentLike>().HasIndex(l => new {l.CommentId, l.UserId}).IsUnique();
    }
}