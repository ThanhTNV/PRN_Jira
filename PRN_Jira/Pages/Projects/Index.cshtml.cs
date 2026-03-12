using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PRN_Jira.Data;
using PRN_Jira.Models;

namespace PRN_Jira.Pages.Projects;

[Authorize]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db)
    {
        _db = db;
    }

    public List<Project> Projects { get; private set; } = new();

    public async Task OnGet()
    {
        await LoadProjects();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        var accountIdStr = User.FindFirstValue("accountId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(accountIdStr, out var accountId))
            return RedirectToPage("/Auth/Login");

        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == id && p.AccountId == accountId);
        if (project != null)
        {
            _db.Projects.Remove(project);
            await _db.SaveChangesAsync();
        }

        return RedirectToPage("Index");
    }

    private async Task LoadProjects()
    {
        var accountIdStr = User.FindFirstValue("accountId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(accountIdStr, out var accountId))
            return;

        Projects = await _db.Projects
            .AsNoTracking()
            .Where(p => p.AccountId == accountId)
            .OrderBy(p => p.JiraProjectId)
            .ToListAsync();
    }
}

