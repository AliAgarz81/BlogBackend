using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BlogBackend.Data;
using BlogBackend.DTOs;
using BlogBackend.Models;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace BlogBackend.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IConfiguration _configuration;
    private readonly IValidator<RegisterDto> _registerValidator;
    private readonly IValidator<LoginDto> _loginValidator;
    private readonly IValidator<PermissionDto> _permissionValidator;

    public AuthController(UserManager<User> userManager, 
        RoleManager<IdentityRole> roleManager, IConfiguration configuration, 
        IValidator<RegisterDto> registerValidator, IValidator<LoginDto> loginValidator
        , IValidator<PermissionDto> permissionValidator)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _configuration = configuration;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
        _permissionValidator = permissionValidator;
    }

    [HttpPost]
    [Route("seed-roles")]
    public async Task<IActionResult> SeedRoles()
    {
        bool isOwnerRoleExits = await _roleManager.RoleExistsAsync(StaticUserRoles.OWNER);
        bool isAdminRoleExits = await _roleManager.RoleExistsAsync(StaticUserRoles.ADMIN);
        bool isUserRoleExits = await _roleManager.RoleExistsAsync(StaticUserRoles.USER);

        if (isOwnerRoleExits && isAdminRoleExits && isUserRoleExits)
            return Ok("Role seeding is already done");
        
        await _roleManager.CreateAsync(new IdentityRole(StaticUserRoles.USER));
        await _roleManager.CreateAsync(new IdentityRole(StaticUserRoles.ADMIN));
        await _roleManager.CreateAsync(new IdentityRole(StaticUserRoles.OWNER));
        
        return Ok("Role seeding done successfully");
    }

    [HttpPost]
    [Route("register")]
    public async Task<IActionResult> Register([FromForm] RegisterDto registerDto)
    {
        ValidationResult result = await _registerValidator.ValidateAsync(registerDto);

if (!result.IsValid) 
{
    foreach (var error in result.Errors)
    {
        ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
    }
    
    return BadRequest(ModelState);
}

string fileName = registerDto.ProfilePic != null
    ? $"{Guid.NewGuid()}{registerDto.ProfilePic.FileName}"
    : "defaultProfilePic876543211234.png";

string filePath = Path.Combine("wwwroot", "Uploads", fileName);

try
{
    var uploadTask = registerDto.ProfilePic != null
        ? Task.Run(async () =>
            {
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await registerDto.ProfilePic.CopyToAsync(stream);
                }
            })
        : Task.CompletedTask;

    var userExistenceTask = _userManager.FindByEmailAsync(registerDto.Email);

    await Task.WhenAll(uploadTask, userExistenceTask);

    if (userExistenceTask.Result != null)
    {
        ModelState.AddModelError("Email", "User with this email already exists");
        return BadRequest(ModelState);
    }

    User newUser = new User()
    {
        Email = registerDto.Email,
        UserName = registerDto.Username,
        ProfilePicUrl = fileName,
        SecurityStamp = Guid.NewGuid().ToString()
    };

    var createUserResult = await _userManager.CreateAsync(newUser, registerDto.Password);
    if (!createUserResult.Succeeded)
    {
        foreach (var error in createUserResult.Errors)
        {
            ModelState.AddModelError("Other", error.Description);
        }

        return BadRequest(ModelState);
    }

    await _userManager.AddToRoleAsync(newUser, StaticUserRoles.USER);
    return Ok("Successfully");
}
catch (Exception ex)
{
    // Handle exceptions (e.g., file system errors) appropriately
    ModelState.AddModelError("Exception", ex.Message);
    return BadRequest(ModelState);
}

    }

    [HttpPost]
    [Route("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
    {
        ValidationResult result = await _loginValidator.ValidateAsync(loginDto);
        if (!result.IsValid)
        {
            return BadRequest("Invalid entry");
        }

        var user = await _userManager.FindByEmailAsync(loginDto.Email);
        if (user is null)
            return Unauthorized("Invalid entry");


        var isPasswordCorrect = await _userManager.CheckPasswordAsync(user, loginDto.Password);
        if (!isPasswordCorrect)
            return Unauthorized("Invalid entry");
        
        var userRoles = await _userManager.GetRolesAsync(user);
        var authClaims = new List<Claim>
        {
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim("JWTID", Guid.NewGuid().ToString())
        };
        foreach (var userRole in userRoles)
        {
            authClaims.Add(new Claim(ClaimTypes.Role, userRole));
        }

        var token = GenerateJWT(authClaims);
        HttpContext.Response.Cookies.Append("jwt", token, new CookieOptions
        {
            Expires = DateTime.Now.AddHours(1),
            HttpOnly = true,
            Secure = true,
            IsEssential = true,
            SameSite = SameSiteMode.None
        });
        return Ok();
    }
    
    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        HttpContext.Response.Cookies.Delete("jwt", new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            IsEssential = true,
            SameSite = SameSiteMode.None
        });

        return Ok();
    }


    [HttpPost]
    [Route("make-admin")]
    public async Task<IActionResult> MakeAdmin([FromBody] PermissionDto permissionDto)
    {
        ValidationResult result = await _permissionValidator.ValidateAsync(permissionDto);
        if (!result.IsValid)
        {
            return BadRequest("Invalid credentials");
        }

        var user = await _userManager.FindByEmailAsync(permissionDto.Email);
        if (user is null)
        {
            return BadRequest("Invalid credentials");
        }

        await _userManager.AddToRoleAsync(user, StaticUserRoles.ADMIN);
        return Ok("Successfull");
    }
    
    [HttpPost]
    [Route("make-owner")]
    public async Task<IActionResult> MakeOwner([FromBody] PermissionDto permissionDto)
    {
        ValidationResult result = await _permissionValidator.ValidateAsync(permissionDto);
        if (!result.IsValid)
        {
            return BadRequest("Invalid credentials");
        }

        var user = await _userManager.FindByEmailAsync(permissionDto.Email);
        if (user is null)
        {
            return BadRequest("Invalid credentials");
        }

        await _userManager.AddToRoleAsync(user, StaticUserRoles.OWNER);
        return Ok("Successfull");
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetUser()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return NotFound();
        var returnFields = new
        {
            UserName = user.UserName,
            Email = user.Email,
            ProfilePic = user.ProfilePicUrl
        };
        return Ok(returnFields);
    }
    [HttpPost("admin")]
    public async Task<IActionResult> AdminLogin([FromBody] LoginDto loginDto)
    {
        ValidationResult result = await _loginValidator.ValidateAsync(loginDto);
        if (!result.IsValid)
        {
            return BadRequest("Invalid entry");
        }

        var user = await _userManager.FindByEmailAsync(loginDto.Email);
        if (user is null)
            return Unauthorized("Invalid entry");


        var isPasswordCorrect = await _userManager.CheckPasswordAsync(user, loginDto.Password);
        if (!isPasswordCorrect)
            return Unauthorized("Invalid entry");
        
        var userRoles = await _userManager.GetRolesAsync(user);
        if (!userRoles.Contains(StaticUserRoles.ADMIN))
        {
            return StatusCode(403);
        }
        var authClaims = new List<Claim>
        {
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim("JWTID", Guid.NewGuid().ToString()),
            new Claim("AdminLogged", "Logged")
        };
        foreach (var userRole in userRoles)
        {
            authClaims.Add(new Claim(ClaimTypes.Role, userRole));
        }

        var token = GenerateJWT(authClaims);
        HttpContext.Response.Cookies.Append("jwt", token, new CookieOptions
        {
            Expires = DateTime.Now.AddHours(1),
            HttpOnly = true,
            Secure = true,
            IsEssential = true,
            SameSite = SameSiteMode.None
        });
        return Ok();
    }

    [HttpGet("get_admin_logged")]
    [Authorize]
    public async Task<IActionResult> GetAdminLogged()
    {
        var adminLogged = User.FindFirst("AdminLogged")?.Value;
        if (adminLogged != "Logged")
        {
            return BadRequest("User is not a admin");
        }
        return Ok(adminLogged);
    }

    private string GenerateJWT(List<Claim> claims)
    {
        var authSecret = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
        var tokenObject = new JwtSecurityToken(
            issuer:_configuration["Jwt:ValidIssuer"],
            audience:_configuration["Jwt:ValidAudience"],
            expires: DateTime.Now.AddHours(1),
            claims:claims,
            signingCredentials:new SigningCredentials(authSecret, SecurityAlgorithms.HmacSha256)
            );
        string token = new JwtSecurityTokenHandler().WriteToken(tokenObject);
        return token;
    }
    
}