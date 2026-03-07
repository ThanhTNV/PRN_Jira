namespace PRN_Jira.DTOs.Auth;

public class RegisterDto
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string JiraBaseUrl { get; set; } = string.Empty;
    public string JiraEmail { get; set; } = string.Empty;
    public string JiraProjectId { get; set; } = string.Empty;
    public string JiraAccessToken { get; set; } = string.Empty;
}
