using PRN_Jira.DTOs.Srs;

namespace PRN_Jira.Services;

public interface IPdfService
{
    byte[] GenerateSrsPdf(SrsVersionDetailDto document, string projectId);
}
