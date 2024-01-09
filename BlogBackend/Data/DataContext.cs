using System.Text.Json;
using System.Text.Json.Serialization;
using BlogBackend.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;


namespace BlogBackend.Data;

public class DataContext : IdentityDbContext<User>
{
    public DataContext(DbContextOptions<DataContext> options) : base(options){}
    public DbSet<Blog> Blogs { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<BlogTag> BlogTags { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tag>()
            .HasIndex(t => new { t.Name, t.Id });

        modelBuilder.Entity<Blog>()
            .HasIndex(b => new { b.Title, b.Id });
        
        modelBuilder.Entity<BlogTag>()
            .HasKey(bt => new { bt.BlogId, bt.TagId });
        
        modelBuilder.Entity<BlogTag>()
            .HasOne(bt => bt.Blog)
            .WithMany(b => b.BlogTags)
            .HasForeignKey(bt => bt.BlogId);

        modelBuilder.Entity<BlogTag>()
            .HasOne(bt => bt.Tag)
            .WithMany(t => t.BlogTags)
            .HasForeignKey(bt => bt.TagId);
        base.OnModelCreating(modelBuilder);
    }
}