using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PRN_Jira.Data;
using PRN_Jira.DTOs.Auth;
using PRN_Jira.Models;
using PRN_Jira.Services;

namespace PRN_Jira.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(AppDbContext db, ITokenService tokenService, ILogger<AuthController> logger)
    {
        _db = db;
        _tokenService = tokenService;
        _logger = logger;
    }

    /// <summary>Register a new account linked to a Jira project.</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password))
            return BadRequest(new { message = "Username and password are required." });

        var account = new Account
        {
            Username = dto.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            JiraAccessToken = dto.JiraAccessToken,
        };

        _db.Accounts.Add(account);
        await _db.SaveChangesAsync();

        // Create first Jira project for this account
        var project = new Project
        {
            AccountId = account.Id,
            JiraBaseUrl = dto.JiraBaseUrl.TrimEnd('/'),
            JiraEmail = dto.JiraEmail,
            JiraProjectId = dto.JiraProjectId
        };
        _db.Projects.Add(project);
        await _db.SaveChangesAsync();

        var token = _tokenService.GenerateToken(account);
        return CreatedAtAction(nameof(Register), new AuthResponseDto
        {
            Token = token,
            Username = account.Username,
            Email = dto.Email,
            ExpiresAt = _tokenService.GetExpiry()
        });
    }

    /// <summary>Login and receive a JWT token.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var username = dto.Email.Trim();
        var account = await _db.Accounts
            .FirstOrDefaultAsync(a => a.Username == username);

        if (account == null || !BCrypt.Net.BCrypt.Verify(dto.Password, account.PasswordHash))
            return Unauthorized(new { message = "Invalid email or password." });

        var token = _tokenService.GenerateToken(account);
        return Ok(new AuthResponseDto
        {
            Token = token,
            Username = account.Username,
            Email = dto.Email,
            ExpiresAt = _tokenService.GetExpiry()
        });
    }
}
