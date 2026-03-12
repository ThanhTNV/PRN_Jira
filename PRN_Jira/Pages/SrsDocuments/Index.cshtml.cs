using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PRN_Jira.Data;
using PRN_Jira.Services;

namespace PRN_Jira.Pages.SrsDocuments;

[Authorize(Policy = "JiraConfigured")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly ISrsService _srsService;

    public IndexModel(AppDbContext db, ISrsService srsService)
    {
        _db = db;
        _srsService = srsService;
    }

    public async Task OnGet()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostCreateSnapshotAsync()
    {
        var accountIdStr = User.FindFirstValue("accountId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(accountIdStr, out var accountId))
            return RedirectToPage("/Auth/Login");

        await LoadAsync();
        if (Projects.Count == 0)
            return RedirectToPage();

        var projectId = SelectedProjectId == Guid.Empty ? Projects[0].Id : SelectedProjectId;
        await _srsService.CreateSnapshotAsync(accountId, projectId, NewDescription ?? string.Empty);

        return RedirectToPage(new { SelectedProjectId = projectId });
    }

    private async Task LoadAsync()
    {
        var accountIdStr = User.FindFirstValue("accountId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(accountIdStr, out var accountId))
            return;

        Projects = await _db.Projects
            .AsNoTracking()
            .Where(p => p.AccountId == accountId)
            .OrderBy(p => p.JiraProjectId)
            .ToListAsync();

        HasAnyProjectWithId = Projects.Any(p => !string.IsNullOrWhiteSpace(p.JiraProjectId));

        if (Projects.Count == 0)
            return;

        var selectedProjectId = SelectedProjectId == Guid.Empty
            ? Projects[0].Id
            : SelectedProjectId;

        SelectedProjectId = selectedProjectId;

        Versions = await _db.SrsDocuments
            .AsNoTracking()
            .Where(d => d.ProjectId == selectedProjectId)
            .OrderByDescending(d => d.VersionNumber)
            .ToListAsync();
    }

    [BindProperty(SupportsGet = true)]
    public Guid SelectedProjectId { get; set; }

    [BindProperty]
    public string? NewDescription { get; set; }

    public List<Models.Project> Projects { get; private set; } = new();
    public List<Models.SrsDocument> Versions { get; private set; } = new();

    /// <summary>True nếu có ít nhất một project đã nhập Jira Project Id/Key.</summary>
    public bool HasAnyProjectWithId { get; private set; }
}

