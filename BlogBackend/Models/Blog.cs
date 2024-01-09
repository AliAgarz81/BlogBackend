using System.ComponentModel.DataAnnotations.Schema;
using BlogBackend.Data.Enums;

namespace BlogBackend.Models;

public class Blog
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Text { get; set; }
    public string CoverImgUrl { get; set; }
    [ForeignKey("User")]
    public string UserId { get; set; }
    public User User { get; set; }
    public ICollection<BlogTag> BlogTags { get; set; }
    public string Category { get; set; }
}