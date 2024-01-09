namespace BlogBackend.DTOs;

public record RegisterDto(string Username, string Email,string Password, string ConfirmPassword,IFormFile? ProfilePic = null );