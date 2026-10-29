namespace Smart_AIUB_Service_Issue.Common;

/// <summary>Settings from the "Issues" section of appsettings.json.</summary>
public class IssueOptions
{
    public const string SectionName = "Issues";

    /// <summary>How many days after "Resolved" the reporter may still reopen the issue.</summary>
    public int ReopenWindowDays { get; set; } = 7;
}
