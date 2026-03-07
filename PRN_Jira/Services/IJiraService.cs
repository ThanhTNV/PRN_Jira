using PRN_Jira.DTOs.Srs;

namespace PRN_Jira.Services;

public interface IJiraService
{
    Task<JiraSnapshotDto> GetProjectSnapshotAsync(
        string baseUrl,
        string email,
        string accessToken,
        string projectId);
}
