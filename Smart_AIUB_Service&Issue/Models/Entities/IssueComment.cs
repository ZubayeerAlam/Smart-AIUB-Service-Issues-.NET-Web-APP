namespace Smart_AIUB_Service_Issue.Models.Entities;

public class IssueComment
{
    public int Id { get; set; }

    public int IssueId { get; set; }
    public Issue Issue { get; set; } = null!;

    public string AuthorId { get; set; } = string.Empty;
    public ApplicationUser Author { get; set; } = null!;

    public string Body { get; set; } = string.Empty;

    /// <summary>Internal notes are visible to Admin and Support Staff only.</summary>
    public bool IsInternal { get; set; }

    public DateTime CreatedAt { get; set; }
}
