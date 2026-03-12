using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using PRN_Jira.Data;

namespace PRN_Jira.Auth;

public sealed class JiraConfiguredHandler : AuthorizationHandler<JiraConfiguredRequirement>
{
    private readonly AppDbContext _db;

    public JiraConfiguredHandler(AppDbContext db)
    {
        _db = db;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        JiraConfiguredRequirement requirement)
    {
        var accountIdStr =
            context.User.FindFirstValue("accountId") ??
            context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(accountIdStr, out var accountId))
            return;

        var account = await _db.Accounts
            .Include(a => a.Projects)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == accountId);

        if (account is null)
            return;

        // Chỉ cần đã nhập Jira Access Token (trong Jira Setup) là được xem SRS.
        // Project (BaseUrl, Email, ProjectId) cấu hình ở trang Projects.
        var hasToken = !string.IsNullOrWhiteSpace(account.JiraAccessToken);

        if (hasToken)
            context.Succeed(requirement);
    }
}

