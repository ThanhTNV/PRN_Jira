namespace PRN_Jira.DTOs.Srs;

public class SrsVersionDetailDto
{
    public Guid Id { get; set; }
    public int VersionNumber { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public JiraSnapshotDto Snapshot { get; set; } = new();
}

public class JiraSnapshotDto
{
    public string ProjectId { get; set; } = string.Empty;
    public string ProjectKey { get; set; } = string.Empty;
    public List<JiraReleaseDto> Releases { get; set; } = new();
    public List<JiraEpicDto> Epics { get; set; } = new();
    public List<JiraUserStoryDto> UserStories { get; set; } = new();
    public DateTime SnapshotTakenAt { get; set; }
}

public class JiraReleaseDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ReleaseDate { get; set; }
    public bool Released { get; set; }
}

public class JiraEpicDto
{
    public string Key { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? AssigneeName { get; set; }
    public string? FixVersion { get; set; }
}

public class JiraUserStoryDto
{
    public string Key { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? AssigneeName { get; set; }
    public string? EpicKey { get; set; }
    public string? FixVersion { get; set; }
    public string? Priority { get; set; }
    public string? StoryPoints { get; set; }
}
