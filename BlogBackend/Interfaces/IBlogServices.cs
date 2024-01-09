using BlogBackend.DTOs;
using BlogBackend.Models;

namespace BlogBackend.Interfaces;

public interface IBlogServices
{
    Task<List<Blog>> GetAllAsync();
    Task<Blog> GetAsync(int Id);
    Task<Blog> GetByNameAsync(string Name);
    Task<List<Blog>> GetByTagAsync(string tag);
    Task CreateAsync(BlogDto blogDto, string imageName, string userId);
    Task<bool> UpdateAsync(int id, BlogDto blogDto, string imageName, string userId);
    Task<bool> DeleteAsync(int Id, string userId);
    Task AdminUpdateAsync(int id, BlogDto blogDto, string imageName);
    Task AdminDeleteAsync(int id);
}