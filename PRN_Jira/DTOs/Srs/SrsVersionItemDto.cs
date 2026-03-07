namespace PRN_Jira.DTOs.Srs;

public class SrsVersionItemDto
{
    public Guid Id { get; set; }
    public int VersionNumber { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
