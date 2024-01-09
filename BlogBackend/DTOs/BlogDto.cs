
namespace BlogBackend.DTOs;

public record BlogDto(string Title, 
    string Text, 
    List<string> Tags, 
    string Category,
    IFormFile? CoverImgUrl = null);