using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRN_Jira.Data;
using PRN_Jira.DTOs.Srs;
using PRN_Jira.Services;

namespace PRN_Jira.Controllers;

[ApiController]
[Route("api/srs")]
[Authorize]
public class SrsController : ControllerBase
{
    private readonly ISrsService _srsService;
    private readonly IPdfService _pdfService;
    private readonly AppDbContext _db;

    public SrsController(ISrsService srsService, IPdfService pdfService, AppDbContext db)
    {
        _srsService = srsService;
        _pdfService = pdfService;
        _db = db;
    }

    private Guid GetAccountId()
    {
        var val = User.FindFirstValue("accountId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.Parse(val!);
    }

    /// <summary>Create a new SRS snapshot from the linked Jira project.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(SrsVersionItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSnapshot([FromBody] CreateSrsDto dto)
    {
        var accountId = GetAccountId();
        try
        {
            var result = await _srsService.CreateSnapshotAsync(accountId, dto.Description);
            return CreatedAtAction(nameof(GetByVersion), new { versionNumber = result.VersionNumber }, result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>List all SRS versions for the authenticated user.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SrsVersionItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVersions()
    {
        var accountId = GetAccountId();
        var versions = await _srsService.GetVersionsAsync(accountId);
        return Ok(versions);
    }

    /// <summary>Get a specific SRS version detail (without PDF).</summary>
    [HttpGet("{versionNumber:int}")]
    [ProducesResponseType(typeof(SrsVersionDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByVersion(int versionNumber)
    {
        var accountId = GetAccountId();
        var detail = await _srsService.GetByVersionAsync(accountId, versionNumber);
        if (detail == null) return NotFound(new { message = $"Version {versionNumber} not found." });
        return Ok(detail);
    }

    /// <summary>Download an SRS version as PDF. Defaults to the latest version if versionNumber is 0.</summary>
    [HttpGet("{versionNumber:int}/download")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadPdf(int versionNumber)
    {
        var accountId = GetAccountId();

        SrsVersionDetailDto? detail;
        if (versionNumber == 0)
            detail = await _srsService.GetLatestAsync(accountId);
        else
            detail = await _srsService.GetByVersionAsync(accountId, versionNumber);

        if (detail == null)
            return NotFound(new { message = "SRS version not found." });

        var account = await _db.Accounts.FindAsync(accountId);
        var pdfBytes = _pdfService.GenerateSrsPdf(detail, account?.JiraProjectId ?? "");
        var fileName = $"SRS_v{detail.VersionNumber}_{DateTime.UtcNow:yyyyMMdd}.pdf";

        return File(pdfBytes, "application/pdf", fileName);
    }

    /// <summary>Download the latest SRS version as PDF.</summary>
    [HttpGet("latest/download")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadLatestPdf()
    {
        var accountId = GetAccountId();
        var detail = await _srsService.GetLatestAsync(accountId);

        if (detail == null)
            return NotFound(new { message = "No SRS versions found." });

        var account = await _db.Accounts.FindAsync(accountId);
        var pdfBytes = _pdfService.GenerateSrsPdf(detail, account?.JiraProjectId ?? "");
        var fileName = $"SRS_v{detail.VersionNumber}_latest_{DateTime.UtcNow:yyyyMMdd}.pdf";

        return File(pdfBytes, "application/pdf", fileName);
    }
}
