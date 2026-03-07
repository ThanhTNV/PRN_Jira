using PRN_Jira.Models;

namespace PRN_Jira.Services;

public interface ITokenService
{
    string GenerateToken(Account account);
    DateTime GetExpiry();
}
