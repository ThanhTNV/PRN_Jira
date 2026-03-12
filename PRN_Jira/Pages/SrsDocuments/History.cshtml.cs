using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PRN_Jira.Data;
using PRN_Jira.Models;
using PRN_Jira.DTOs.Srs;
using PRN_Jira.Services;

namespace PRN_Jira.Pages.SrsDocuments;

[Authorize(Policy = "JiraConfigured")]
public class HistoryModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly ISrsService _srsService;
    private readonly IPdfService _pdfService;

    public HistoryModel(AppDbContext db, ISrsService srsService, IPdfService pdfService)
    {
        _db = db;
        _srsService = srsService;
        _pdfService = pdfService;
    }

    [BindProperty(SupportsGet = true)]
    public int? versionNumber { get; set; }

    public string? Error { get; private set; }
    public Account? Account { get; private set; }
    public List<SrsDocument> Versions { get; private set; } = new();

    public int? SelectedVersionNumber { get; private set; }
    public SrsDocument? SelectedDoc { get; private set; }
    public JiraSnapshotDto? Snapshot { get; private set; }

    public async Task<IActionResult> OnGet()
    {
        var accountIdStr = User.FindFirstValue("accountId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(accountIdStr, out var accountId))
            return Redirect("/Auth/Login");

        Account = await _db.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == accountId);
        if (Account is null) return Page();

        Versions = await _db.SrsDocuments
            .AsNoTracking()
            .Where(d => d.AccountId == accountId)
            .OrderByDescending(d => d.VersionNumber)
            .ToListAsync();

        if (Versions.Count == 0) return Page();

        SelectedVersionNumber = versionNumber ?? Versions[0].VersionNumber;
        SelectedDoc = Versions.FirstOrDefault(v => v.VersionNumber == SelectedVersionNumber);

        if (SelectedDoc is null) return Page();

        Snapshot = System.Text.Json.JsonSerializer.Deserialize<JiraSnapshotDto>(SelectedDoc.SnapshotJson) ?? new JiraSnapshotDto();
        return Page();
    }

    /// <summary>Trả file PDF SRS để tải về, không chuyển hướng (gọi từ JS fetch).</summary>
    public async Task<IActionResult> OnGetDownloadPdf()
    {
        var accountIdStr = User.FindFirstValue("accountId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(accountIdStr, out var accountId))
            return Unauthorized();

        var v = versionNumber ?? 0;
        if (v <= 0) return BadRequest();

        var detail = await _srsService.GetByVersionAsync(accountId, v);
        if (detail == null) return NotFound();

        var project = await _db.Projects
            .AsNoTracking()
            .Where(p => p.AccountId == accountId)
            .OrderBy(p => p.JiraProjectId)
            .Select(p => p.JiraProjectId)
            .FirstOrDefaultAsync();

        var pdfBytes = _pdfService.GenerateSrsPdf(detail, project ?? "");
        var fileName = $"SRS_v{v}.pdf";
        return File(pdfBytes, "application/pdf", fileName);
    }
}

