namespace BlogBackend.Models;

public class BlogTag
{
    public Blog Blog { get; set; }
    public Tag Tag { get; set; }
    public int BlogId { get; set; }
    public int TagId { get; set; }
}