namespace BlogBackend.Models;

public class Tag
{
    public int Id { get; set; }
    public string Name { get; set; }
    public ICollection<BlogTag> BlogTags { get; set; }
}