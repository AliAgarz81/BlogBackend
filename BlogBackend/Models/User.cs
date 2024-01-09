using Microsoft.AspNetCore.Identity;

namespace BlogBackend.Models;

public class User : IdentityUser
{
    public string? ProfilePicUrl { get; set; }
    public ICollection<Blog> Blogs { get; set; }
}