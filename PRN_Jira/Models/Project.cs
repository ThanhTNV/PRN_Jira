namespace PRN_Jira.Models;

public class Project
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AccountId { get; set; }

    public string JiraProjectId { get; set; } = string.Empty;
    public string JiraBaseUrl { get; set; } = string.Empty;
    public string JiraEmail { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Account Account { get; set; } = null!;
    public ICollection<SrsDocument> SrsDocuments { get; set; } = new List<SrsDocument>();
}

