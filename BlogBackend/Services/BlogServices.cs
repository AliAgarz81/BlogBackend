using BlogBackend.Data;
using BlogBackend.DTOs;
using BlogBackend.Interfaces;
using BlogBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace BlogBackend.Services;

public class BlogServices : IBlogServices
{
    private readonly DataContext _context;

    public BlogServices(DataContext context)
    {
        _context = context;
    }

    public async Task<List<Blog>> GetAllAsync()
    {
        return await _context.Blogs.OrderBy(b => b.Id).ToListAsync();
    }

    public async Task<Blog> GetAsync(int Id)
    {
        return await _context.Blogs.FirstOrDefaultAsync(b => b.Id == Id);
    }

    public async Task<Blog> GetByNameAsync(string Name)
    {
        return await _context.Blogs.FirstOrDefaultAsync(b => b.Title == Name);
    }

    public async Task<List<Blog>> GetByTagAsync(string tag)
    {
        List<Blog> blogs = new List<Blog>();
        var currentTag = await _context.Tags.FirstOrDefaultAsync(t => t.Name == tag);
        var blogTags = await _context.BlogTags.Where(blogTag => blogTag.TagId == currentTag.Id)
            .Include(blogTag => blogTag.Blog).ToListAsync();
        if (!blogTags.Any())
        {
            return blogs;
        }
        foreach (var blogTag in blogTags)
        {
            blogs.Add(blogTag.Blog);
        }

        return blogs;
    }

    public async Task CreateAsync(BlogDto blogDto, string imageName, string userId)
    {
        List<Tag> newTags = new List<Tag>();
        List<BlogTag> newBlogTags = new List<BlogTag>();

        foreach (string tag in blogDto.Tags)
        {
            var existingTag = await _context.Tags.FirstOrDefaultAsync(t => t.Name == tag);

            if (existingTag is null)
            {
                var newTag = new Tag { Name = tag };
                newTags.Add(newTag);
            }
        }

        if (newTags.Any())
        {
            await _context.Tags.AddRangeAsync(newTags);
        }
        var blog = new Blog
        {
            Title = blogDto.Title,
            Text = blogDto.Text,
            CoverImgUrl = blogDto.CoverImgUrl != null ? $"{imageName}{blogDto.CoverImgUrl.FileName}" : " ",
            Category = blogDto.Category,
            UserId = userId
        };
        await _context.Blogs.AddAsync(blog);
        await _context.SaveChangesAsync();
        foreach (var currentTag in blogDto.Tags)
        {
            var existingTag = await _context.Tags.FirstOrDefaultAsync(t => t.Name == currentTag);
            var tag = new BlogTag()
            {
                BlogId = blog.Id,
                TagId = existingTag.Id
            };
            newBlogTags.Add(tag);
        }
        await _context.BlogTags.AddRangeAsync(newBlogTags);
        await _context.SaveChangesAsync();
    }


    public async Task UpdateAsync(int id, BlogDto blogDto, string imageName, string userId)
    {
        List<Tag> newTags = new List<Tag>();
        List<BlogTag> newBlogTags = new List<BlogTag>();
        foreach (string tag in blogDto.Tags)
        {
            var existingTag = await _context.Tags.FirstOrDefaultAsync(t => t.Name == tag);

            if (existingTag is null)
            {
                var newTag = new Tag { Name = tag };
                newTags.Add(newTag);
            }
        }

        if (newTags.Any())
        {
            await _context.Tags.AddRangeAsync(newTags);
            await _context.SaveChangesAsync();
        }

        Blog? blog = await _context.Blogs.Include(b => b.BlogTags).FirstOrDefaultAsync(b => b.Id == id);
        _context.BlogTags.RemoveRange(blog.BlogTags);
        foreach (var currentTag in blogDto.Tags)
        {
            var existingTag = await _context.Tags.FirstOrDefaultAsync(t => t.Name == currentTag);
            var tag = new BlogTag()
            {
                BlogId = blog.Id,
                TagId = existingTag.Id
            };
            newBlogTags.Add(tag);
        }

        await _context.BlogTags.AddRangeAsync(newBlogTags);
        blog.Title = blogDto.Title;
        blog.Text = blogDto.Text;
        blog.CoverImgUrl = blogDto.CoverImgUrl != null ? $"{imageName}{blogDto.CoverImgUrl.FileName}" : " ";
        blog.Category = blogDto.Category;
        blog.UserId = userId;
        _context.Update(blog);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(int Id, string userId)
    {
        var blog = await _context.Blogs
            .Include(b => b.BlogTags)
            .FirstOrDefaultAsync(b => b.Id == Id);

        if (blog?.UserId != userId)
        {
            return false;
        }
        if (blog != null)
        {
            _context.Blogs.Remove(blog);
            await _context.SaveChangesAsync();
            return true;
        }
        return false;
    }
}