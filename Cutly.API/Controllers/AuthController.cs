
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Cutly.API.Models.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Cutly.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _configuration = configuration;
    }

    public record RegisterRequest(
        string FirstName,
        string LastName,
        string Email,
        string Password,
        string AccountType);

    public record LoginRequest(
        string Email,
        string Password);

    [HttpPost("register")]
    public async Task<IActionResult> Register(
     [FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName) ||
            string.IsNullOrWhiteSpace(request.LastName) ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password) ||
            string.IsNullOrWhiteSpace(request.AccountType))
        {
            return BadRequest(new
            {
                message = "All fields are required."
            });
        }

        var normalizedAccountType = request.AccountType.Trim();

        string role = normalizedAccountType.ToLowerInvariant() switch
        {
            "customer" => "Customer",
            "barber" => "Employee",
            _ => string.Empty
        };

        if (string.IsNullOrEmpty(role))
        {
            return BadRequest(new
            {
                message = "AccountType must be Customer or Barber."
            });
        }

        var existingUser = await _userManager.FindByEmailAsync(
            request.Email.Trim());

        if (existingUser != null)
        {
            return Conflict(new
            {
                message = "An account with this email already exists."
            });
        }

        var user = new ApplicationUser
        {
            UserName = request.Email.Trim(),
            Email = request.Email.Trim(),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim()
        };

        var result = await _userManager.CreateAsync(
            user,
            request.Password);

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                errors = result.Errors.Select(e => e.Description)
            });
        }

        var roleResult = await _userManager.AddToRoleAsync(user, role);

        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);

            return StatusCode(500, new
            {
                message = "Could not assign the account role."
            });
        }

        return Ok(new
        {
            message = "Registration successful.",
            userId = user.Id,
            email = user.Email,
            accountType = normalizedAccountType
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(
            request.Email.Trim());

        if (user is null ||
            !await _userManager.CheckPasswordAsync(
                user, request.Password))
        {
            return Unauthorized(new
            {
                message = "Invalid email or password."
            });
        }

        var keyValue = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException(
                "JWT key is missing.");

        var issuer = _configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException(
                "JWT issuer is missing.");

        var audience = _configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException(
                "JWT audience is missing.");

        // Вземаме всички роли на потребителя от Identity.
        var userRoles = await _userManager.GetRolesAsync(user);

        // Създаваме основните JWT claims.
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName!),
            new(ClaimTypes.Email, user.Email!)
        };

        // Добавяме ролите в JWT токена.
        claims.AddRange(
            userRoles.Select(role =>
                new Claim(ClaimTypes.Role, role))
        );

        var signingKey = new SymmetricSecurityKey(
            Convert.FromBase64String(keyValue));

        var credentials = new SigningCredentials(
            signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: credentials);

        return Ok(new
        {
            accessToken = new JwtSecurityTokenHandler()
                .WriteToken(token),
            expiresAtUtc = token.ValidTo,
            userId = user.Id,
            email = user.Email
        });
        
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId);

        if (user is null)
        {
            return NotFound();
        }

        return Ok(new
        {
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName
        });
    }

}
