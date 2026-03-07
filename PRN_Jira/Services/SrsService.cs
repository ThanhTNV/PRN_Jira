using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using PRN_Jira.Data;
using PRN_Jira.DTOs.Srs;
using PRN_Jira.Models;

namespace PRN_Jira.Services;

public class SrsService : ISrsService
{
    private readonly AppDbContext _db;
    private readonly IJiraService _jiraService;

    public SrsService(AppDbContext db, IJiraService jiraService)
    {
        _db = db;
        _jiraService = jiraService;
    }

    public async Task<SrsVersionItemDto> CreateSnapshotAsync(Guid accountId, string description)
    {
        var account = await _db.Accounts.FindAsync(accountId)
            ?? throw new InvalidOperationException("Account not found.");

        // Fetch Jira data
        var snapshot = await _jiraService.GetProjectSnapshotAsync(
            account.JiraBaseUrl,
            account.JiraEmail,
            account.JiraAccessToken,
            account.JiraProjectId);

        snapshot.ProjectKey = account.JiraProjectId;

        // Determine next version number
        var maxVersion = await _db.SrsDocuments
            .Where(d => d.AccountId == accountId)
            .MaxAsync(d => (int?)d.VersionNumber) ?? 0;

        var doc = new SrsDocument
        {
            AccountId = accountId,
            VersionNumber = maxVersion + 1,
            Description = description,
            SnapshotJson = JsonSerializer.Serialize(snapshot),
            CreatedAt = DateTime.UtcNow
        };

        _db.SrsDocuments.Add(doc);
        await _db.SaveChangesAsync();

        return new SrsVersionItemDto
        {
            Id = doc.Id,
            VersionNumber = doc.VersionNumber,
            Description = doc.Description,
            CreatedAt = doc.CreatedAt
        };
    }

    public async Task<IEnumerable<SrsVersionItemDto>> GetVersionsAsync(Guid accountId)
    {
        return await _db.SrsDocuments
            .Where(d => d.AccountId == accountId)
            .OrderByDescending(d => d.VersionNumber)
            .Select(d => new SrsVersionItemDto
            {
                Id = d.Id,
                VersionNumber = d.VersionNumber,
                Description = d.Description,
                CreatedAt = d.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<SrsVersionDetailDto?> GetByVersionAsync(Guid accountId, int versionNumber)
    {
        var doc = await _db.SrsDocuments
            .FirstOrDefaultAsync(d => d.AccountId == accountId && d.VersionNumber == versionNumber);

        return doc == null ? null : MapToDetail(doc);
    }

    public async Task<SrsVersionDetailDto?> GetLatestAsync(Guid accountId)
    {
        var doc = await _db.SrsDocuments
            .Where(d => d.AccountId == accountId)
            .OrderByDescending(d => d.VersionNumber)
            .FirstOrDefaultAsync();

        return doc == null ? null : MapToDetail(doc);
    }

    private static SrsVersionDetailDto MapToDetail(SrsDocument doc)
    {
        var snapshot = JsonSerializer.Deserialize<JiraSnapshotDto>(doc.SnapshotJson) ?? new JiraSnapshotDto();
        return new SrsVersionDetailDto
        {
            Id = doc.Id,
            VersionNumber = doc.VersionNumber,
            Description = doc.Description,
            CreatedAt = doc.CreatedAt,
            Snapshot = snapshot
        };
    }
}
