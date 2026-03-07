using PRN_Jira.DTOs.Srs;

namespace PRN_Jira.Services;

public interface ISrsService
{
    Task<SrsVersionItemDto> CreateSnapshotAsync(Guid accountId, string description);
    Task<IEnumerable<SrsVersionItemDto>> GetVersionsAsync(Guid accountId);
    Task<SrsVersionDetailDto?> GetByVersionAsync(Guid accountId, int versionNumber);
    Task<SrsVersionDetailDto?> GetLatestAsync(Guid accountId);
}
