using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using PRN_Jira.DTOs.Srs;

namespace PRN_Jira.Services;

public class JiraService : IJiraService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<JiraService> _logger;

    public JiraService(IHttpClientFactory httpClientFactory, ILogger<JiraService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<JiraSnapshotDto> GetProjectSnapshotAsync(
        string baseUrl, string email, string accessToken, string projectId)
    {
        var client = _httpClientFactory.CreateClient("Jira");
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{email}:{accessToken}"));
        client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", credentials);
        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        var snapshot = new JiraSnapshotDto
        {
            ProjectId = projectId,
            SnapshotTakenAt = DateTime.UtcNow
        };

        // Fetch Releases (Versions)
        var relResp = await client.GetAsync($"rest/api/3/project/{projectId}/versions");
        if (relResp.IsSuccessStatusCode)
        {
            var json = await relResp.Content.ReadAsStringAsync();
            var releases = JsonDocument.Parse(json).RootElement;
            foreach (var r in releases.EnumerateArray())
            {
                snapshot.Releases.Add(new JiraReleaseDto
                {
                    Id = r.TryGetProperty("id", out var rid) ? rid.GetString() ?? "" : "",
                    Name = r.TryGetProperty("name", out var rn) ? rn.GetString() ?? "" : "",
                    Description = r.TryGetProperty("description", out var rd) ? rd.GetString() ?? "" : "",
                    Status = r.TryGetProperty("released", out var rreleased) && rreleased.GetBoolean() ? "Released" : "Unreleased",
                    Released = r.TryGetProperty("released", out var rrel2) && rrel2.GetBoolean(),
                    ReleaseDate = r.TryGetProperty("releaseDate", out var rdate) ? rdate.GetString() : null,
                });
            }
        }
        else if (relResp.StatusCode != System.Net.HttpStatusCode.NotFound) // 404 might just mean no versions
        {
            var err = await relResp.Content.ReadAsStringAsync();
            throw new Exception($"Jira API Releases Error: {relResp.StatusCode} - {err}");
        }

        // Fetch Epics using POST /rest/api/3/search/jql
        var epicQuery = new
        {
            jql = $"project = \"{projectId}\" AND issuetype = Epic",
            maxResults = 200,
            fields = new[] { "summary", "description", "status", "assignee", "fixVersions" }
        };
        var epicResp = await client.PostAsJsonAsync("rest/api/3/search/jql", epicQuery);

        if (epicResp.IsSuccessStatusCode)
        {
            var json = await epicResp.Content.ReadAsStringAsync();
            var root = JsonDocument.Parse(json).RootElement;
            JsonElement issuesArray;
            if (root.TryGetProperty("issues", out var issues)) issuesArray = issues;
            else if (root.TryGetProperty("values", out var values)) issuesArray = values;
            else issuesArray = root; // Fallback in case the root IS the array or something else

            if (issuesArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var issue in issuesArray.EnumerateArray())
                {
                    var key = issue.TryGetProperty("key", out var k) ? k.GetString() ?? "" : "";
                    if (!issue.TryGetProperty("fields", out var fields)) continue;

                    snapshot.Epics.Add(new JiraEpicDto
                    {
                        Key = key,
                        Summary = fields.TryGetProperty("summary", out var s) && s.ValueKind == JsonValueKind.String ? s.GetString() ?? "" : "",
                        Description = ExtractDescription(fields),
                        Status = fields.TryGetProperty("status", out var st) && st.TryGetProperty("name", out var sn)
                            ? sn.GetString() ?? "" : "",
                        AssigneeName = fields.TryGetProperty("assignee", out var a) && a.ValueKind != JsonValueKind.Null
                            && a.TryGetProperty("displayName", out var dn) ? dn.GetString() : null,
                        FixVersion = ExtractFixVersion(fields),
                    });
                }
            }
            else
            {
                _logger.LogWarning("Jira API Epics Response did not contain 'issues' or 'values' array. JSON: {Json}", json);
            }
        }
        else
        {
            var err = await epicResp.Content.ReadAsStringAsync();
            throw new Exception($"Jira API Epics Error: {epicResp.StatusCode} - {err}");
        }

        // Fetch User Stories and Bugs (since users often treat bugs as stories in SRS)
        // using POST /rest/api/3/search/jql
        var storyQuery = new
        {
            jql = $"project = \"{projectId}\" AND issuetype IN (Story, Bug, Task)",
            maxResults = 500,
            fields = new[] { "summary", "description", "status", "assignee", "priority", "fixVersions", "customfield_10014", "story_points", "customfield_10016", "issuetype" }
        };
        var storyResp = await client.PostAsJsonAsync("rest/api/3/search/jql", storyQuery);

        if (storyResp.IsSuccessStatusCode)
        {
            var json = await storyResp.Content.ReadAsStringAsync();
            var root = JsonDocument.Parse(json).RootElement;
            JsonElement issuesArray;
            if (root.TryGetProperty("issues", out var issues)) issuesArray = issues;
            else if (root.TryGetProperty("values", out var values)) issuesArray = values;
            else issuesArray = root;

            if (issuesArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var issue in issuesArray.EnumerateArray())
                {
                    var key = issue.TryGetProperty("key", out var k) ? k.GetString() ?? "" : "";
                    if (!issue.TryGetProperty("fields", out var fields)) continue;

                    string? epicKey = null;
                    if (fields.TryGetProperty("customfield_10014", out var ef) && ef.ValueKind != JsonValueKind.Null)
                        epicKey = ef.GetString();

                    string? storyPoints = null;
                    if (fields.TryGetProperty("customfield_10016", out var sp) && sp.ValueKind != JsonValueKind.Null)
                        storyPoints = sp.ValueKind == JsonValueKind.Number ? sp.GetDecimal().ToString() : sp.GetString();

                    // Optional: Get issue type name to append to summary if it's a bug or task
                    string issueTypeName = "Story";
                    if (fields.TryGetProperty("issuetype", out var it) && it.TryGetProperty("name", out var itn))
                        issueTypeName = itn.GetString() ?? "Story";

                    var summary = fields.TryGetProperty("summary", out var s) && s.ValueKind == JsonValueKind.String ? s.GetString() ?? "" : "";
                    if (issueTypeName != "Story") summary = $"[{issueTypeName.ToUpper()}] {summary}";

                    snapshot.UserStories.Add(new JiraUserStoryDto
                    {
                        Key = key,
                        Summary = summary,
                        Description = ExtractDescription(fields),
                        Status = fields.TryGetProperty("status", out var st) && st.TryGetProperty("name", out var sn)
                            ? sn.GetString() ?? "" : "",
                        AssigneeName = fields.TryGetProperty("assignee", out var a) && a.ValueKind != JsonValueKind.Null
                            && a.TryGetProperty("displayName", out var dn) ? dn.GetString() : null,
                        EpicKey = epicKey,
                        FixVersion = ExtractFixVersion(fields),
                        Priority = fields.TryGetProperty("priority", out var p) && p.TryGetProperty("name", out var pn)
                            ? pn.GetString() : null,
                        StoryPoints = storyPoints,
                    });
                }
            }
            else
            {
                _logger.LogWarning("Jira API User Stories Response did not contain 'issues' or 'values' array. JSON: {Json}", json);
            }
        }
        else
        {
            var err = await storyResp.Content.ReadAsStringAsync();
            throw new Exception($"Jira API User Stories Error: {storyResp.StatusCode} - {err}");
        }

        return snapshot;
    }

    private static string ExtractDescription(JsonElement fields)
    {
        if (!fields.TryGetProperty("description", out var desc) || desc.ValueKind == JsonValueKind.Null)
            return "";

        // Jira description can be Atlassian Document Format (ADF) or plain string
        if (desc.ValueKind == JsonValueKind.String)
            return desc.GetString() ?? "";

        // Try to extract plain text from ADF content
        try
        {
            return ExtractTextFromAdf(desc);
        }
        catch
        {
            return "";
        }
    }

    private static string ExtractTextFromAdf(JsonElement node)
    {
        var sb = new StringBuilder();
        if (node.TryGetProperty("text", out var text))
            sb.Append(text.GetString());

        if (node.TryGetProperty("content", out var content))
        {
            foreach (var child in content.EnumerateArray())
            {
                var childText = ExtractTextFromAdf(child);
                if (!string.IsNullOrEmpty(childText))
                    sb.Append(childText).Append(' ');
            }
        }
        return sb.ToString().Trim();
    }

    private static string? ExtractFixVersion(JsonElement fields)
    {
        if (!fields.TryGetProperty("fixVersions", out var fv) || fv.ValueKind != JsonValueKind.Array)
            return null;
        foreach (var v in fv.EnumerateArray())
        {
            if (v.TryGetProperty("name", out var vn))
                return vn.GetString();
        }
        return null;
    }
}
