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
public class CreateModel : PageModel
{
    private readonly AppDbContext _db;

    public CreateModel(AppDbContext db)
    {
        _db = db;
    }

    public string? Error { get; private set; }

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

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPost()
    {
        if (!ModelState.IsValid)
        {
            Error = "Vui lòng nhập đủ thông tin.";
            return Page();
        }

        var accountIdStr = User.FindFirstValue("accountId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(accountIdStr, out var accountId))
            return Redirect("/Auth/Login");

        var exists = await _db.Projects.AnyAsync(p =>
            p.AccountId == accountId && p.JiraProjectId == Input.JiraProjectId);
        if (exists)
        {
            Error = "ProjectId này đã tồn tại cho account hiện tại.";
            return Page();
        }

        var project = new Project
        {
            AccountId = accountId,
            JiraBaseUrl = Input.JiraBaseUrl.Trim().TrimEnd('/'),
            JiraEmail = Input.JiraEmail.Trim(),
            JiraProjectId = Input.JiraProjectId.Trim()
        };

        _db.Projects.Add(project);
        await _db.SaveChangesAsync();

        return RedirectToPage("Index");
    }
}

