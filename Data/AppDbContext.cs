using Microsoft.EntityFrameworkCore;
using SocialMediaAppBackend.Models;

namespace SocialMediaAppBackend.Data;


public class AppDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Post> Posts { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<Like> Likes { get; set; }
    public DbSet<CommentLike> CommentLikes { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<Follow> Follows { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {

    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
        modelBuilder.Entity<User>().HasIndex(u => u.Username).IsUnique();

        modelBuilder.Entity<Like>().HasIndex(l => new { l.PostId, l.UserId }).IsUnique();

        modelBuilder.Entity<CommentLike>().HasIndex(l => new { l.CommentId, l.UserId }).IsUnique();

        modelBuilder.Entity<RefreshToken>().HasIndex(x => x.Token).IsUnique();

        modelBuilder.Entity<Follow>().HasIndex(f => new { f.FollowerId, f.FollowedUserId }).IsUnique();
    }
}