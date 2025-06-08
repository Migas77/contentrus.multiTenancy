using Microsoft.AspNetCore.Mvc;
using ContentRus.TenantManagement.Models;
using ContentRus.TenantManagement.Services;
using ContentRus.TenantManagement.Configs;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;

/// <summary>
/// API endpoints for managing user authentication.
/// </summary>
namespace ContentRus.TenantManagement.Controllers;

[ApiController]
[Route("api/user")]
public class UserController : ControllerBase
{
    private readonly UserService _userService;
    private readonly TenantService _tenantService;
    private readonly JwtSettings _jwtSettings;

    public UserController(UserService userService, TenantService tenantService, IOptions<JwtSettings> jwtOptions)
    {
        _userService = userService;
        _tenantService = tenantService;
        _jwtSettings = jwtOptions.Value;
    }

    private string GenerateJwtToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_jwtSettings.SecretKey);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("TenantId", user.TenantId.ToString())
            }),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private string GetUsername(User user)
    {
        var tenant = _tenantService.GetTenant(user.TenantId);
        if (tenant != null)
        {
            if (tenant.Name != null)
            {
                return tenant.Name;
            }
        }
        return user.Email;
    }

    /// <summary>
    /// Registers a new user and creates a new tenant.
    /// </summary>
    /// <param name="authRequest">User email and password.</param>
    /// <returns>JWT token and username.</returns>
    [HttpPost("register")]
    public IActionResult RegisterUser([FromBody] AuthRequest authRequest)
    {
        var tenant = _tenantService.CreateTenant();
        var user = _userService.CreateUser(authRequest.Email, authRequest.Password, tenant.Id);

        var token = GenerateJwtToken(user);
        return Ok(new { token, username = GetUsername(user) });
    }

    /// <summary>
    /// Logs in an existing user.
    /// </summary>
    /// <param name="authRequest">User email and password.</param>
    /// <returns>JWT token and username if credentials are valid.</returns>
    [HttpPost("login")]
    public IActionResult LoginUser([FromBody] AuthRequest authRequest)
    {
        var user = _userService.ValidateUserCredentials(authRequest.Email, authRequest.Password);
        if (user == null)
        {
            return Unauthorized();
        }

        var token = GenerateJwtToken(user);
        return Ok(new { token, username = GetUsername(user) });
    }

    /// <summary>
    /// Updates a user's password.
    /// </summary>
    /// <param name="id">User ID.</param>
    /// <param name="newPassword">New password string.</param>
    /// <returns>No content if successful, NotFound otherwise.</returns>
    [HttpPut("{id:int}/password")]
    public IActionResult UpdateUserPassword(int id, [FromBody] string newPassword)
    {
        var updated = _userService.UpdateUserPassword(id, newPassword);
        return updated ? NoContent() : NotFound();
    }

    /// <summary>
    /// Gets user details by ID.
    /// </summary>
    /// <param name="id">User ID.</param>
    /// <returns>User details if found.</returns>
    [HttpGet("{id:int}")]
    public IActionResult GetUser(int id)
    {
        var user = _userService.GetUser(id);
        return user is not null ? Ok(user) : NotFound();
    }


    /// <summary>
    /// Validates the user's JWT token.
    /// </summary>
    /// <returns>Boolean indicating whether the token is valid.</returns>
    [Authorize]
    [HttpGet("validate")]
    public IActionResult ValidateToken()
    {
        return Ok(new { valid = true });
    }
}
