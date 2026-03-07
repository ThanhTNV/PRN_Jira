namespace PRN_Jira.Models;

public class Account
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string JiraProjectId { get; set; } = string.Empty;
    public string JiraAccessToken { get; set; } = string.Empty;
    public string JiraBaseUrl { get; set; } = string.Empty;
    public string JiraEmail { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<SrsDocument> SrsDocuments { get; set; } = new List<SrsDocument>();
}
