using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using BlogBackend.DTOs;
using BlogBackend.Interfaces;
using BlogBackend.Models;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ValidationResult = FluentValidation.Results.ValidationResult;

namespace BlogBackend.Controllers;

[Route("api/[controller]")]
[ApiController]
public class BlogController : ControllerBase
{
    private readonly IBlogServices _blogServices;
    private readonly IValidator<BlogDto> _validator;

    public BlogController(IBlogServices blogServices, IValidator<BlogDto> validator)
    {
        _blogServices = blogServices;
        _validator = validator;
    }

    [HttpGet]
    public async Task<IActionResult> GetBlogs()
    {
        var blogs = await _blogServices.GetAllAsync();
        return Ok(blogs);
    }

    [HttpGet("{blogId}")]
    public async Task<IActionResult> GetBlogById(int blogId)
    {
        var blog = _blogServices.GetAsync(blogId);
        if (blog is null)
            return BadRequest();
        return Ok(blog);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateBlog([FromForm] BlogDto blogDto)
    {
        ValidationResult result = await _validator.ValidateAsync(blogDto);
        if (!result.IsValid)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }
    
            return BadRequest(ModelState);
        }

        var checkExists = await _blogServices.GetByNameAsync(blogDto.Title);
        if (checkExists is not null)
        {
            return BadRequest("Blog already exists with this title");
        }

        string imageName = Guid.NewGuid().ToString();
        string fileName = blogDto.CoverImgUrl != null ? $"{imageName}{blogDto.CoverImgUrl.FileName}" : " ";
        string filePath = Path.Combine("wwwroot", "Uploads", fileName);
        var uploadTask = blogDto.CoverImgUrl != null
            ? Task.Run(async () =>
            {
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await blogDto.CoverImgUrl.CopyToAsync(stream);
                }
            })
            : Task.CompletedTask;
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        await _blogServices.CreateAsync(blogDto, imageName, userId);
        await Task.WhenAll(uploadTask);
        return Ok();
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateBlog(int id, [FromForm] BlogDto blogDto)
    {
        ValidationResult result = await _validator.ValidateAsync(blogDto);
        if (!result.IsValid)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }
    
            return BadRequest(ModelState);
        }

        var checkBlog =  _blogServices.GetAsync(id);
        if (checkBlog.Result is null)
        {
            return BadRequest("Blog doesn't exist");
        }
        string imageName = Guid.NewGuid().ToString();
        string fileName = blogDto.CoverImgUrl != null ? $"{imageName}{blogDto.CoverImgUrl.FileName}" : " ";
        string filePath = Path.Combine("wwwroot", "Uploads", fileName);
        var uploadTask = blogDto.CoverImgUrl != null
            ? Task.Run(async () =>
            {
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await blogDto.CoverImgUrl.CopyToAsync(stream);
                }
            })
            : Task.CompletedTask;
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        await Task.WhenAll(uploadTask, checkBlog);
        await _blogServices.UpdateAsync(id, blogDto, imageName, userId);
        return Ok();
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteBlog(int id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var result = _blogServices.DeleteAsync(id, userId);
        if (await result)
        {
            return Ok();
        }

        return BadRequest();
    }

    [HttpGet("tag/{tag}")]
    public async Task<IActionResult> GetBlogsByTagName(string tag)
    {
        var result = await _blogServices.GetByTagAsync(tag);
        return Ok(result);
    }
}