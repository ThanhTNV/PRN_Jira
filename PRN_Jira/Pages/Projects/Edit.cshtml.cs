using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PRN_Jira.Data;
using PRN_Jira.Models;

namespace PRN_Jira.Pages.Projects;

[Authorize]
public class EditModel : PageModel
{
    private readonly AppDbContext _db;

    public EditModel(AppDbContext db)
    {
        _db = db;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public string? Error { get; private set; }
    public Project? Project { get; private set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required]
        public string JiraBaseUrl { get; set; } = "";

        [Required]
        public string JiraEmail { get; set; } = "";

        [Required]
        public string JiraProjectId { get; set; } = "";
    }

    public async Task<IActionResult> OnGet()
    {
        var accountId = await GetAccountId();
        if (accountId == null) return RedirectToPage("/Auth/Login");

        Project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == Id && p.AccountId == accountId.Value);
        if (Project == null) return Page();

        Input = new InputModel
        {
            JiraBaseUrl = Project.JiraBaseUrl,
            JiraEmail = Project.JiraEmail,
            JiraProjectId = Project.JiraProjectId
        };
        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        var accountId = await GetAccountId();
        if (accountId == null) return RedirectToPage("/Auth/Login");

        if (!ModelState.IsValid)
        {
            Project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == Id && p.AccountId == accountId.Value);
            Error = "Vui lòng nhập đủ thông tin.";
            return Page();
        }

        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == Id && p.AccountId == accountId.Value);
        if (project == null)
        {
            Error = "Project không tồn tại.";
            return RedirectToPage("Index");
        }

        var duplicate = await _db.Projects.AnyAsync(p =>
            p.AccountId == accountId.Value && p.JiraProjectId == Input.JiraProjectId.Trim() && p.Id != Id);
        if (duplicate)
        {
            Error = "ProjectId này đã tồn tại trong account.";
            Project = project;
            return Page();
        }

        project.JiraBaseUrl = Input.JiraBaseUrl.Trim().TrimEnd('/');
        project.JiraEmail = Input.JiraEmail.Trim();
        project.JiraProjectId = Input.JiraProjectId.Trim();
        await _db.SaveChangesAsync();

        return RedirectToPage("Index");
    }

    private Task<Guid?> GetAccountId()
    {
        var accountIdStr = User.FindFirstValue("accountId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Task.FromResult(Guid.TryParse(accountIdStr, out var id) ? (Guid?)id : null);
    }
}
