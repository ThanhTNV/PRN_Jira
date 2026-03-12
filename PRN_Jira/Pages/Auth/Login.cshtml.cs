using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PRN_Jira.Data;

namespace PRN_Jira.Pages.Auth;

public class LoginModel : PageModel
{
    private readonly AppDbContext _db;

    public LoginModel(AppDbContext db)
    {
        _db = db;
    }

    public string? Error { get; private set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required]
        public string Username { get; set; } = "";

        [Required]
        public string Password { get; set; } = "";
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPost()
    {
        if (!ModelState.IsValid)
        {
            Error = "Vui lòng nhập email/password hợp lệ.";
            return Page();
        }

        var username = Input.Username.Trim();
        var account = await _db.Accounts.FirstOrDefaultAsync(a => a.Username == username);

        if (account is null || !BCrypt.Net.BCrypt.Verify(Input.Password, account.PasswordHash))
        {
            Error = "Sai email hoặc password.";
            return Page();
        }

        var claims = new List<Claim>
        {
            new("accountId", account.Id.ToString()),
            new(ClaimTypes.NameIdentifier, account.Id.ToString()),
            new(ClaimTypes.Name, account.Username),
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties { IsPersistent = true });

        // Bắt buộc setup Jira (có token + ít nhất 1 project) trước khi dùng chức năng Jira/SRS
        var hasToken = !string.IsNullOrWhiteSpace(account.JiraAccessToken);
        var hasProject = await _db.Projects.AnyAsync(p => p.AccountId == account.Id);

        var needsSetup = !hasToken || !hasProject;
        return Redirect(needsSetup ? "/Jira/Setup" : "/");
    }
}

