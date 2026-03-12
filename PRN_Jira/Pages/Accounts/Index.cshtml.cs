using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PRN_Jira.Data;
using PRN_Jira.Models;

namespace PRN_Jira.Pages.Accounts;

[Authorize]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db)
    {
        _db = db;
    }

    public Account? Account { get; private set; }
    public List<Project> Projects { get; private set; } = new();

    public async Task OnGet()
    {
        var accountIdStr = User.FindFirstValue("accountId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(accountIdStr, out var accountId))
            return;

        Account = await _db.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == accountId);
        Projects = await _db.Projects
            .AsNoTracking()
            .Where(p => p.AccountId == accountId)
            .OrderBy(p => p.JiraProjectId)
            .ToListAsync();
    }
}

