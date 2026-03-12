using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PRN_Jira.Data;
using PRN_Jira.Models;

namespace PRN_Jira.Pages.Auth;

public class RegisterModel : PageModel
{
    private readonly AppDbContext _db;

    public RegisterModel(AppDbContext db)
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

        // Giữ lại field để không phải sửa view, nhưng không lưu vào DB
        public string Email { get; set; } = "";

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
            Error = "Vui lòng nhập thông tin hợp lệ.";
            return Page();
        }

        var username = Input.Username.Trim();
        if (await _db.Accounts.AnyAsync(a => a.Username == username))
        {
            Error = "Username đã tồn tại.";
            return Page();
        }

        var account = new Account
        {
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(Input.Password),
            JiraAccessToken = ""
        };

        _db.Accounts.Add(account);
        await _db.SaveChangesAsync();

        var claims = new List<Claim>
        {
            new("accountId", account.Id.ToString()),
            new(ClaimTypes.NameIdentifier, account.Id.ToString()),
            new(ClaimTypes.Name, account.Username),
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = true });

        return Redirect("/Jira/Setup");
    }
}

