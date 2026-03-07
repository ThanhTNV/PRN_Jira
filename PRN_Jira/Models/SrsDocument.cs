namespace PRN_Jira.Models;

public class SrsDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AccountId { get; set; }
    public int VersionNumber { get; set; }
    public string Description { get; set; } = string.Empty;
    public string SnapshotJson { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Account Account { get; set; } = null!;
}
