using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PRN_Jira.Data;

namespace PRN_Jira.Pages.Jira;

[Authorize]
public class SetupModel : PageModel
{
    private readonly AppDbContext _db;

    public SetupModel(AppDbContext db)
    {
        _db = db;
    }

    public string? Error { get; private set; }
    public string? Message { get; private set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required]
        public string JiraBaseUrl { get; set; } = "";

        [Required]
        public string JiraEmail { get; set; } = "";

        [Required]
        public string JiraAccessToken { get; set; } = "";
    }

    public async Task<IActionResult> OnGet()
    {
        var accountIdStr = User.FindFirstValue("accountId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(accountIdStr, out var accountId))
            return Redirect("/Auth/Login");

        var account = await _db.Accounts
            .Include(a => a.Projects)
            .FirstOrDefaultAsync(a => a.Id == accountId);
        if (account is null)
            return Redirect("/Auth/Login");

        var project = account.Projects.FirstOrDefault();

        Input = new InputModel
        {
            JiraBaseUrl = project?.JiraBaseUrl ?? "",
            JiraEmail = project?.JiraEmail ?? "",
            JiraAccessToken = account.JiraAccessToken
        };

        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        if (!ModelState.IsValid)
        {
            Error = "Vui lòng nhập đủ thông tin Jira.";
            return Page();
        }

        var accountIdStr = User.FindFirstValue("accountId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(accountIdStr, out var accountId))
            return Redirect("/Auth/Login");

        var account = await _db.Accounts
            .Include(a => a.Projects)
            .FirstOrDefaultAsync(a => a.Id == accountId);
        if (account is null)
            return Redirect("/Auth/Login");

        account.JiraAccessToken = Input.JiraAccessToken.Trim();

        var project = account.Projects.FirstOrDefault();
        if (project is null)
        {
            project = new Models.Project
            {
                AccountId = account.Id
            };
            _db.Projects.Add(project);
        }

        project.JiraBaseUrl = Input.JiraBaseUrl.Trim().TrimEnd('/');
        project.JiraEmail = Input.JiraEmail.Trim();
        // JiraProjectId sẽ được cấu hình chi tiết trong màn Projects (CRUD)

        await _db.SaveChangesAsync();
        Message = "Đã lưu Jira config. Bạn có thể dùng chức năng SRS/Jira.";
        return Page();
    }
}

